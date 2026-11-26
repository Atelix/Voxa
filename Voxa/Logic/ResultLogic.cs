using Microsoft.EntityFrameworkCore;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

/// <summary>
/// What the board and the hall may see. While an item is open only its number of ballots is shown; the choices become
/// visible when it closes, so nobody can watch a vote change as it runs.
/// </summary>
public sealed class ResultLogic(VoxaContext context, TimeProvider time) : IResultLogic
{
    public async Task<LiveModel> LiveAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await context.Meetings.AsNoTracking()
                                   .Include(static m => m.Items).ThenInclude(static i => i.Ballots)
                                   .AsSplitQuery()
                                   .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken)
                      ?? throw new ObjectNotFoundException(nameof(Meeting), meetingId);
        var voters = await context.Voters.CountAsync(v => v.MeetingId == meetingId, cancellationToken);
        var used = await context.VotingCodes.CountAsync(c => c.MeetingId == meetingId && c.Uses.Count > 0, cancellationToken);
        var now = time.GetUtcNow().UtcDateTime;
        return new LiveModel
        {
            Meeting = Mapping.Summary(meeting, voters, used),
            AdvanceOpenNow = meeting.Status == MeetingStatus.Ready && meeting.AdvanceVoting && meeting.AdvanceOpensAt <= now && now < meeting.StartsAt,
            ScreenItemId = meeting.ScreenItemId,
            Items = [.. meeting.Items.OrderBy(static i => i.Position).Select(Mapping.Live)],
        };
    }

    public async Task<ResultModel> ResultAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await LoadAsync(itemId, cancellationToken);
        EnsureClosed(item);
        return ResultCalculator.Calculate(item);
    }

    public async Task<ResultModel> SetPaperVotesAsync(Guid itemId, PaperVotesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await LoadAsync(itemId, cancellationToken, tracking: true);
        EnsureClosed(item);
        MeetingLogic.EnsureNotArchived(item.Meeting);

        var counts = request.Options.GroupBy(static o => o.OptionId).ToDictionary(static g => g.Key, static g => g.Sum(static o => o.Count));
        if (counts.Keys.Any(id => item.Options.All(o => o.Id != id)) || counts.Values.Any(static c => c < 0) || request.Ballots < 0)
        {
            throw new DomainValidationException(Texts.Get("Error.PaperOptions"));
        }

        int CountFor(Func<ItemOption, bool> match) => item.Options.Where(match).Sum(o => counts.GetValueOrDefault(o.Id));
        var ballots = request.Ballots;
        if (item.Kind == AgendaItemKind.Proposal)
        {
            // Every paper ballot on a proposal has exactly one choice.
            ballots = CountFor(static _ => true);
        }
        else
        {
            var blank = CountFor(static o => o.Role == OptionRole.Blank);
            var candidates = CountFor(static o => o.Role == OptionRole.Candidate);
            if (blank > ballots || candidates > (ballots - blank) * item.Seats || item.Options.Any(o => counts.GetValueOrDefault(o.Id) > ballots))
            {
                throw new DomainValidationException(Texts.Get("Error.PaperTotals"));
            }
        }

        SetTally(item.Ballots, item.Id, ballots);
        foreach (var option in item.Options)
        {
            var tally = option.Tallies.FirstOrDefault(static t => t.Channel == VoteChannel.Paper);
            var count = counts.GetValueOrDefault(option.Id);
            if (tally is null)
            {
                option.Tallies.Add(new OptionTally { ItemOptionId = option.Id, Channel = VoteChannel.Paper, Count = count, Version = Guid.NewGuid() });
            }
            else
            {
                tally.Count = count;
                tally.Version = Guid.NewGuid();
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return ResultCalculator.Calculate(item);
    }

    public async Task SetScreenAsync(Guid meetingId, ScreenRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await context.Meetings.Include(static m => m.Items).FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken)
                      ?? throw new ObjectNotFoundException(nameof(Meeting), meetingId);
        if (request.ItemId is { } itemId && meeting.Items.All(i => i.Id != itemId || i.State != AgendaItemState.Closed))
        {
            throw new DomainValidationException(Texts.Get("Error.ScreenClosedOnly"));
        }

        meeting.ScreenItemId = request.ItemId;
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScreenModel> ScreenAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await context.Meetings.AsNoTracking()
                                   .Include(static m => m.Items).ThenInclude(static i => i.Ballots)
                                   .Include(static m => m.Items).ThenInclude(static i => i.Options).ThenInclude(static o => o.Tallies)
                                   .AsSplitQuery()
                                   .FirstOrDefaultAsync(m => m.Id == meetingId && m.Status == MeetingStatus.Ready, cancellationToken)
                      ?? throw new ObjectNotFoundException(nameof(Meeting), meetingId);
        var open = meeting.Items.Where(static i => i.State == AgendaItemState.Open).OrderBy(static i => i.Position).FirstOrDefault();
        var shown = meeting.Items.FirstOrDefault(i => i.Id == meeting.ScreenItemId && i.State == AgendaItemState.Closed);
        return new ScreenModel
        {
            MeetingName = meeting.Name,
            OpenItem = open is null ? null : Mapping.Live(open),
            Result = shown is null ? null : ResultCalculator.Calculate(shown),
        };
    }

    private static void SetTally(ICollection<BallotTally> tallies, Guid itemId, int count)
    {
        var tally = tallies.FirstOrDefault(static t => t.Channel == VoteChannel.Paper);
        if (tally is null)
        {
            tallies.Add(new BallotTally { AgendaItemId = itemId, Channel = VoteChannel.Paper, Count = count, Version = Guid.NewGuid() });
        }
        else
        {
            tally.Count = count;
            tally.Version = Guid.NewGuid();
        }
    }

    private static void EnsureClosed(AgendaItem item)
    {
        if (item.State != AgendaItemState.Closed)
        {
            throw new DomainForbiddenException(Texts.Get("Error.ResultHidden"));
        }
    }

    private async Task<AgendaItem> LoadAsync(Guid itemId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = context.AgendaItems.Include(static i => i.Meeting)
                           .Include(static i => i.Ballots)
                           .Include(static i => i.Options).ThenInclude(static o => o.Tallies)
                           .AsSplitQuery();
        return await (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
               ?? throw new ObjectNotFoundException(nameof(AgendaItem), itemId);
    }
}

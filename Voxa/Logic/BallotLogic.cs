using Microsoft.EntityFrameworkCore;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

/// <summary>
/// Voting with a code. A vote is stored only by raising counters (ballots per item and channel, choices per option and
/// channel) and by marking the code as used on that item, in one transaction. No row describes a single vote, so the
/// database cannot tell who chose what, nor in which order votes arrived.
/// </summary>
public sealed class BallotLogic(VoxaContext context, ICodeService codes, IAttemptLimiter limiter, IClientIdentity client, TimeProvider time) : IBallotLogic
{
    /// <summary>Wrong codes from one device (browser) before it waits ten minutes.</summary>
    public const int DeviceFailures = 5;

    /// <summary>Wrong codes from one network address. Looser, because a whole hall can share one address.</summary>
    public const int AddressFailures = 60;

    /// <summary>Tries when many votes arrive at once and the same counters change underneath.</summary>
    private const int SaveAttempts = 25;

    public async Task<BallotSessionModel> SessionAsync(BallotCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = await FindCodeAsync(request.Code, cancellationToken);
        var meeting = code.Meeting;
        var now = time.GetUtcNow().UtcDateTime;
        var used = code.Uses.Select(static u => u.AgendaItemId).ToHashSet();
        return new BallotSessionModel
        {
            MeetingName = meeting.Name,
            StartsAt = meeting.StartsAt,
            Location = meeting.Location,
            AdvanceOpensAt = meeting.AdvanceVoting ? meeting.AdvanceOpensAt : null,
            Phase = Phase(meeting, now),
            Items = [.. meeting.Items.OrderBy(static i => i.Position).Select(i => new BallotItemModel
            {
                Id = i.Id,
                Position = i.Position,
                Title = i.Title,
                Description = i.Description,
                Kind = i.Kind,
                Seats = i.Seats,
                Status = used.Contains(i.Id) ? BallotItemStatus.Voted
                    : i.State == AgendaItemState.Closed ? BallotItemStatus.Closed
                    : IsVotable(meeting, i, now) ? BallotItemStatus.Open
                    : BallotItemStatus.Pending,
                Options = Mapping.Options(i),
            })],
        };
    }

    public async Task VoteAsync(VoteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = await FindCodeAsync(request.Code, cancellationToken);
        var item = code.Meeting.Items.FirstOrDefault(i => i.Id == request.ItemId)
                   ?? throw new ObjectNotFoundException(nameof(AgendaItem), request.ItemId);
        var now = time.GetUtcNow().UtcDateTime;
        if (!IsVotable(code.Meeting, item, now))
        {
            throw new DomainForbiddenException(Texts.Get("Error.ItemNotOpen"));
        }

        if (code.Uses.Any(u => u.AgendaItemId == item.Id))
        {
            throw new DomainConflictException(Texts.Get("Error.AlreadyVoted"));
        }

        var chosen = ValidChoice(item, request.OptionIds);
        var channel = now < code.Meeting.StartsAt ? VoteChannel.Advance : VoteChannel.Hall;
        var codeId = code.Id;
        context.ChangeTracker.Clear();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await WriteVoteAsync(codeId, item.Id, chosen, channel, cancellationToken);
                return;
            }
            catch (DbUpdateException)
            {
                // Another vote changed the same counter, or a second request with this code got there first.
                context.ChangeTracker.Clear();
                if (await context.CodeUses.AnyAsync(u => u.VotingCodeId == codeId && u.AgendaItemId == item.Id, cancellationToken))
                {
                    throw new DomainConflictException(Texts.Get("Error.AlreadyVoted"));
                }

                if (attempt >= SaveAttempts)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(System.Security.Cryptography.RandomNumberGenerator.GetInt32(5, 20 * attempt)), cancellationToken);
            }
        }
    }

    /// <summary>Whether a code may vote on the item now: it is open live, or advance voting runs for it.</summary>
    public static bool IsVotable(Meeting meeting, AgendaItem item, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        ArgumentNullException.ThrowIfNull(item);
        if (meeting.Status != MeetingStatus.Ready)
        {
            return false;
        }

        return item.State == AgendaItemState.Open
               || (item.State == AgendaItemState.Pending && meeting.AdvanceVoting && item.AdvanceVoting
                   && meeting.AdvanceOpensAt <= now && now < meeting.StartsAt);
    }

    public static VotingPhase Phase(Meeting meeting, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        if (meeting.Items.Count > 0 && meeting.Items.All(static i => i.State == AgendaItemState.Closed))
        {
            return VotingPhase.Ended;
        }

        if (now >= meeting.StartsAt || meeting.Items.Any(static i => i.State == AgendaItemState.Open))
        {
            return VotingPhase.Meeting;
        }

        return meeting.AdvanceVoting && meeting.AdvanceOpensAt <= now ? VotingPhase.Advance : VotingPhase.NotYetOpen;
    }

    private async Task WriteVoteAsync(Guid codeId, Guid itemId, IReadOnlyList<Guid> chosen, VoteChannel channel, CancellationToken cancellationToken)
    {
        _ = context.CodeUses.Add(new CodeUse { VotingCodeId = codeId, AgendaItemId = itemId });

        var ballot = await context.BallotTallies.FirstOrDefaultAsync(t => t.AgendaItemId == itemId && t.Channel == channel, cancellationToken);
        if (ballot is null)
        {
            _ = context.BallotTallies.Add(new BallotTally { AgendaItemId = itemId, Channel = channel, Count = 1, Version = Guid.NewGuid() });
        }
        else
        {
            ballot.Count++;
            ballot.Version = Guid.NewGuid();
        }

        var tallies = await context.OptionTallies.Where(t => chosen.Contains(t.ItemOptionId) && t.Channel == channel).ToListAsync(cancellationToken);
        foreach (var optionId in chosen)
        {
            var tally = tallies.FirstOrDefault(t => t.ItemOptionId == optionId);
            if (tally is null)
            {
                _ = context.OptionTallies.Add(new OptionTally { ItemOptionId = optionId, Channel = channel, Count = 1, Version = Guid.NewGuid() });
            }
            else
            {
                tally.Count++;
                tally.Version = Guid.NewGuid();
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The chosen option ids, checked against the item: one choice for a proposal; for an election up to the
    /// number of seats of different candidates, or only the blank option.</summary>
    private static List<Guid> ValidChoice(AgendaItem item, IReadOnlyList<Guid> optionIds)
    {
        var ids = optionIds.Distinct().ToList();
        var options = item.Options.Where(o => ids.Contains(o.Id)).ToList();
        if (options.Count != ids.Count || ids.Count != optionIds.Count || ids.Count == 0)
        {
            throw new DomainValidationException(Texts.Get("Error.ChooseFromOptions"));
        }

        if (item.Kind == AgendaItemKind.Proposal)
        {
            return ids.Count == 1 ? ids : throw new DomainValidationException(Texts.Get("Error.ChooseOne"));
        }

        if (options.Any(static o => o.Role == OptionRole.Blank))
        {
            return ids.Count == 1 ? ids : throw new DomainValidationException(Texts.Get("Error.BlankNoCandidates"));
        }

        return ids.Count <= item.Seats ? ids : throw new DomainValidationException(Texts.Format("Error.TooManyCandidates", item.Seats));
    }

    private async Task<VotingCode> FindCodeAsync(string? input, CancellationToken cancellationToken)
    {
        var keys = new[] { new AttemptKey("code-device:" + client.Device, DeviceFailures), new AttemptKey("code-address:" + client.Address, AddressFailures) };
        limiter.EnsureAllowed(keys);
        var normalized = codes.Normalize(input);
        var hash = normalized is null ? null : codes.Hash(normalized);
        var code = hash is null
            ? null
            : await context.VotingCodes
                           .Include(static c => c.Uses)
                           .Include(static c => c.Meeting).ThenInclude(static m => m.Items).ThenInclude(static i => i.Options)
                           .AsSplitQuery()
                           .FirstOrDefaultAsync(c => c.Hash == hash, cancellationToken);
        if (code is null || code.Meeting.Status == MeetingStatus.Archived)
        {
            limiter.RecordFailure(keys);
            throw new ObjectNotFoundException(Texts.Get("Error.CodeUnknown"));
        }

        return code;
    }
}

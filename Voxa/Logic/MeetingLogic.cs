using Microsoft.EntityFrameworkCore;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

public sealed class MeetingLogic(VoxaContext context, ClubSettingsStore club, TimeProvider time) : IMeetingLogic
{
    public async Task<IReadOnlyList<MeetingSummaryModel>> ListAsync(CancellationToken cancellationToken)
    {
        var meetings = await context.Meetings.AsNoTracking().Include(static m => m.Items)
                                    .OrderByDescending(static m => m.StartsAt).ToListAsync(cancellationToken);
        var voters = await context.Voters.GroupBy(static v => v.MeetingId).Select(static g => new { g.Key, Count = g.Count() })
                                  .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);
        var used = await CodesUsedAsync(null, cancellationToken);
        return [.. meetings.Select(m => Mapping.Summary(m, voters.GetValueOrDefault(m.Id), used.GetValueOrDefault(m.Id)))];
    }

    public async Task<MeetingModel> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await LoadAsync(id, cancellationToken);
        return await ToModelAsync(meeting, cancellationToken);
    }

    public async Task<MeetingModel> CreateAsync(MeetingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = new Meeting { Name = string.Empty, CreatedAt = time.GetUtcNow().UtcDateTime, Status = MeetingStatus.Draft };
        await ApplyAsync(meeting, request, cancellationToken);
        _ = context.Meetings.Add(meeting);
        _ = await context.SaveChangesAsync(cancellationToken);
        return await ToModelAsync(meeting, cancellationToken);
    }

    public async Task<MeetingModel> UpdateAsync(Guid id, MeetingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await LoadAsync(id, cancellationToken);
        EnsureNotArchived(meeting);
        await ApplyAsync(meeting, request, cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);
        return await ToModelAsync(meeting, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await LoadAsync(id, cancellationToken);
        if (meeting.Status == MeetingStatus.Ready)
        {
            throw new DomainConflictException(Texts.Get("Error.MeetingCodesSent"));
        }

        _ = context.Meetings.Remove(meeting);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeetingModel> CopyAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await LoadAsync(id, cancellationToken);
        var settings = await club.GetAsync(cancellationToken);
        var startsAt = source.StartsAt.AddYears(1);
        var copy = new Meeting
        {
            Name = NextName(source.Name),
            Kind = source.Kind,
            StartsAt = startsAt,
            Location = source.Location,
            Language = source.Language,
            AdvanceVoting = source.AdvanceVoting,
            AdvanceOpensAt = source.AdvanceVoting ? EarliestAdvance(startsAt, settings.AdvanceVotingMaxDays) : null,
            Status = MeetingStatus.Draft,
            CreatedAt = time.GetUtcNow().UtcDateTime,
            Items = [.. source.Items.OrderBy(static i => i.Position).Select(static i => new AgendaItem
            {
                Position = i.Position,
                Title = i.Title,
                Description = i.Description,
                Kind = i.Kind,
                Majority = i.Majority,
                Seats = i.Seats,
                AdvanceVoting = i.AdvanceVoting,
                Options = [.. i.Options.Select(static o => new ItemOption { Position = o.Position, Label = o.Label, Note = o.Note, Role = o.Role })],
            })],
        };
        _ = context.Meetings.Add(copy);
        _ = await context.SaveChangesAsync(cancellationToken);
        return await ToModelAsync(copy, cancellationToken);
    }

    public async Task<MeetingModel> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await LoadAsync(id, cancellationToken);
        EnsureNotArchived(meeting);
        var used = await CodesUsedAsync(id, cancellationToken);
        foreach (var item in meeting.Items.Where(static i => i.State != AgendaItemState.Closed))
        {
            item.State = AgendaItemState.Closed;
        }

        meeting.CodesUsedAtArchive = used.GetValueOrDefault(id);
        meeting.Status = MeetingStatus.Archived;
        meeting.ArchivedAt = time.GetUtcNow().UtcDateTime;
        meeting.ScreenItemId = null;
        context.VotingCodes.RemoveRange(await context.VotingCodes.Where(c => c.MeetingId == id).ToListAsync(cancellationToken));
        context.Voters.RemoveRange(meeting.Voters);
        _ = await context.SaveChangesAsync(cancellationToken);
        return await ToModelAsync(meeting, cancellationToken);
    }

    public async Task<ExportModel> ExportAsync(CancellationToken cancellationToken)
    {
        var meetings = await context.Meetings.AsNoTracking()
                                    .Include(static m => m.Items).ThenInclude(static i => i.Options).ThenInclude(static o => o.Tallies)
                                    .Include(static m => m.Items).ThenInclude(static i => i.Ballots)
                                    .AsSplitQuery()
                                    .OrderBy(static m => m.StartsAt).ToListAsync(cancellationToken);
        var used = await CodesUsedAsync(null, cancellationToken);
        return new ExportModel
        {
            ExportedAt = time.GetUtcNow().UtcDateTime,
            Meetings = [.. meetings.Select(m => new MeetingExportModel
            {
                Meeting = Mapping.Summary(m, 0, used.GetValueOrDefault(m.Id)),
                // Only closed items have a result; open ones would reveal a running vote.
                Results = [.. m.Items.Where(static i => i.State == AgendaItemState.Closed).OrderBy(static i => i.Position).Select(ResultCalculator.Calculate)],
            })],
        };
    }

    public async Task WipeAsync(WipeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // The word in any language counts, so a board that switches language is not locked out of its own data.
        var confirm = request.Confirm?.Trim();
        if (!Languages.Supported.Any(l => string.Equals(confirm, Texts.GetIn(Texts.Culture(l), "Meeting.WipeWord"), StringComparison.Ordinal)))
        {
            throw new DomainValidationException(Texts.Format("Error.WipeConfirm", Texts.Get("Meeting.WipeWord")));
        }

        // Codes first: their uses refer to agenda items as well as codes.
        context.CodeUses.RemoveRange(await context.CodeUses.ToListAsync(cancellationToken));
        context.VotingCodes.RemoveRange(await context.VotingCodes.ToListAsync(cancellationToken));
        context.Meetings.RemoveRange(await context.Meetings.ToListAsync(cancellationToken));
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The start of the day <paramref name="maxDays"/> days before the meeting's day, in the club's time zone.</summary>
    public static DateTime EarliestAdvance(DateTime startsAt, int maxDays)
    {
        var localDay = ClubTime.ToLocal(startsAt).Date.AddDays(-maxDays);
        return ClubTime.ToUtc(localDay);
    }

    internal async Task<Meeting> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Meetings
                     .Include(static m => m.Items).ThenInclude(static i => i.Options)
                     .Include(static m => m.Items).ThenInclude(static i => i.Ballots)
                     .Include(static m => m.Voters)
                     .AsSplitQuery()
                     .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
        ?? throw new ObjectNotFoundException(nameof(Meeting), id);

    internal static void EnsureNotArchived(Meeting meeting)
    {
        if (meeting.Status == MeetingStatus.Archived)
        {
            throw new DomainConflictException(Texts.Get("Error.MeetingArchived"));
        }
    }

    private async Task ApplyAsync(Meeting meeting, MeetingRequest request, CancellationToken cancellationToken)
    {
        var name = Mapping.Clean(request.Name) ?? throw new DomainValidationException(Texts.Get("Error.MeetingNameRequired"));
        var language = Mapping.Clean(request.Language);
        if (language is not null && !Languages.IsSupported(language))
        {
            throw new DomainValidationException(Texts.Format("Error.LanguageInvalid", string.Join(", ", Languages.Supported)));
        }

        var startsAt = DateTime.SpecifyKind(request.StartsAt.ToUniversalTime(), DateTimeKind.Utc);
        var settings = await club.GetAsync(cancellationToken);
        DateTime? opensAt = null;
        if (request.AdvanceVoting)
        {
            var earliest = EarliestAdvance(startsAt, settings.AdvanceVotingMaxDays);
            opensAt = request.AdvanceOpensAt is { } requested ? DateTime.SpecifyKind(requested.ToUniversalTime(), DateTimeKind.Utc) : earliest;
            if (opensAt < earliest)
            {
                throw new DomainValidationException(Texts.Format("Error.AdvanceTooEarly", settings.AdvanceVotingMaxDays));
            }

            if (opensAt >= startsAt)
            {
                throw new DomainValidationException(Texts.Get("Error.AdvanceAfterStart"));
            }
        }

        meeting.Name = name;
        meeting.Kind = request.Kind;
        meeting.StartsAt = startsAt;
        meeting.Location = Mapping.Clean(request.Location);
        meeting.Language = language;
        meeting.AdvanceVoting = request.AdvanceVoting;
        meeting.AdvanceOpensAt = opensAt;
    }

    private async Task<MeetingModel> ToModelAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        var settings = await club.GetAsync(cancellationToken);
        var used = meeting.Id == Guid.Empty ? [] : await CodesUsedAsync(meeting.Id, cancellationToken);
        var summary = Mapping.Summary(meeting, meeting.Voters.Count, used.GetValueOrDefault(meeting.Id));
        return new MeetingModel
        {
            Id = summary.Id,
            Name = summary.Name,
            Kind = summary.Kind,
            StartsAt = summary.StartsAt,
            Location = summary.Location,
            Language = summary.Language,
            Status = summary.Status,
            AdvanceVoting = summary.AdvanceVoting,
            AdvanceOpensAt = summary.AdvanceOpensAt,
            ItemCount = summary.ItemCount,
            VoterCount = summary.VoterCount,
            CodesIssued = summary.CodesIssued,
            CodesUsed = summary.CodesUsed,
            ArchivedAt = summary.ArchivedAt,
            EarliestAdvanceOpensAt = EarliestAdvance(meeting.StartsAt, settings.AdvanceVotingMaxDays),
            ScreenItemId = meeting.ScreenItemId,
            Items = [.. meeting.Items.OrderBy(static i => i.Position).Select(Mapping.Item)],
        };
    }

    /// <summary>Per meeting, how many of its codes voted at least once.</summary>
    private async Task<Dictionary<Guid, int>> CodesUsedAsync(Guid? meetingId, CancellationToken cancellationToken) =>
        await context.VotingCodes.Where(c => meetingId == null || c.MeetingId == meetingId)
                     .Where(static c => c.Uses.Count > 0)
                     .GroupBy(static c => c.MeetingId)
                     .Select(static g => new { g.Key, Count = g.Count() })
                     .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);

    /// <summary>"ALV 2027" becomes "ALV 2028"; other names get "(copy)" in the board's language.</summary>
    private static string NextName(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"(19|20)\d{2}");
        if (!match.Success)
        {
            return Texts.Format("Meeting.CopyName", name);
        }

        var year = int.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) + 1;
        return string.Concat(name.AsSpan(0, match.Index), year.ToString(System.Globalization.CultureInfo.InvariantCulture), name.AsSpan(match.Index + match.Length));
    }
}

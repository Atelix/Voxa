using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;

namespace Voxa.Logic;

/// <summary>Entity to model conversions shared by the logic classes.</summary>
internal static class Mapping
{
    public static MeetingSummaryModel Summary(Meeting meeting, int voterCount, int codesUsed) => new()
    {
        Id = meeting.Id,
        Name = meeting.Name,
        Kind = meeting.Kind,
        StartsAt = meeting.StartsAt,
        Location = meeting.Location,
        Language = meeting.Language,
        Status = meeting.Status,
        AdvanceVoting = meeting.AdvanceVoting,
        AdvanceOpensAt = meeting.AdvanceOpensAt,
        ItemCount = meeting.Items.Count,
        VoterCount = voterCount,
        CodesIssued = meeting.CodesIssued,
        CodesUsed = meeting.CodesUsedAtArchive ?? codesUsed,
        ArchivedAt = meeting.ArchivedAt,
    };

    public static AgendaItemModel Item(AgendaItem item) => new()
    {
        Id = item.Id,
        Position = item.Position,
        Title = item.Title,
        Description = item.Description,
        Kind = item.Kind,
        Majority = item.Majority,
        Seats = item.Seats,
        State = item.State,
        AdvanceVoting = item.AdvanceVoting,
        Options = Options(item),
        Ballots = item.Ballots.Sum(static b => b.Count),
        Locked = IsLocked(item),
    };

    public static IReadOnlyList<OptionModel> Options(AgendaItem item) =>
        [.. item.Options.OrderBy(static o => o.Position).Select(static o => new OptionModel { Id = o.Id, Label = o.Label, Note = o.Note, Role = o.Role })];

    /// <summary>An item that was opened or has votes keeps its options: changing them would change what was voted on.</summary>
    public static bool IsLocked(AgendaItem item) => item.State != AgendaItemState.Pending || item.Ballots.Any(static b => b.Count > 0);

    public static LiveItemModel Live(AgendaItem item) => new()
    {
        Id = item.Id,
        Position = item.Position,
        Title = item.Title,
        Kind = item.Kind,
        State = item.State,
        AdvanceVoting = item.AdvanceVoting,
        Ballots = ResultCalculator.Count(item.Ballots.Select(static b => (b.Channel, b.Count))),
    };

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

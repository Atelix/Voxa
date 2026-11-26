namespace Voxa.Persistence.Entity;

public class Meeting
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public MeetingKind Kind { get; set; }

    public DateTime StartsAt { get; set; }

    public string? Location { get; set; }

    /// <summary>Members may vote online before the meeting, from <see cref="AdvanceOpensAt"/> until it starts.</summary>
    public bool AdvanceVoting { get; set; }

    public DateTime? AdvanceOpensAt { get; set; }

    public MeetingStatus Status { get; set; }

    /// <summary>How many voting codes were issued (mail and paper). Kept after archiving for the turnout.</summary>
    public int CodesIssued { get; set; }

    /// <summary>How many codes voted at least once. Saved when the meeting is archived, when the codes are deleted.</summary>
    public int? CodesUsedAtArchive { get; set; }

    /// <summary>The closed item whose result the hall screen shows, if the board chose one.</summary>
    public int? ScreenItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public ICollection<AgendaItem> Items { get; set; } = [];

    public ICollection<Voter> Voters { get; set; } = [];
}

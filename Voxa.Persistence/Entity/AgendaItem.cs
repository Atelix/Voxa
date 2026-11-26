namespace Voxa.Persistence.Entity;

public class AgendaItem
{
    public Guid Id { get; set; }

    public Guid MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public int Position { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public AgendaItemKind Kind { get; set; }

    public MajorityRule Majority { get; set; }

    /// <summary>For an election: how many people are elected, and so how many candidates a ballot may name.</summary>
    public int Seats { get; set; }

    public AgendaItemState State { get; set; }

    /// <summary>Whether this item may be voted on before the meeting (when the meeting allows advance voting).</summary>
    public bool AdvanceVoting { get; set; }

    public ICollection<ItemOption> Options { get; set; } = [];

    public ICollection<BallotTally> Ballots { get; set; } = [];
}

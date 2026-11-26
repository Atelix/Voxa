namespace Voxa.Persistence.Entity;

/// <summary>
/// The number of ballots cast on an item through one channel. Votes are only ever stored as these counters, so no
/// row describes a single vote and nothing links a choice to a code, a member or a moment.
/// </summary>
public class BallotTally
{
    public int AgendaItemId { get; set; }

    public AgendaItem AgendaItem { get; set; } = null!;

    public VoteChannel Channel { get; set; }

    public int Count { get; set; }

    /// <summary>Optimistic concurrency token: two votes at the same time cannot both write the same old count.</summary>
    public Guid Version { get; set; }
}

/// <summary>The number of times an option was chosen through one channel.</summary>
public class OptionTally
{
    public int ItemOptionId { get; set; }

    public ItemOption ItemOption { get; set; } = null!;

    public VoteChannel Channel { get; set; }

    public int Count { get; set; }

    public Guid Version { get; set; }
}

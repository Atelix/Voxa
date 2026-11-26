namespace Voxa.Persistence.Entity;

/// <summary>An annual general meeting or an extra members' meeting.</summary>
public enum MeetingKind
{
    Annual = 0,
    Extra = 1,
}

/// <summary>Where a meeting is in its life: prepared, codes sent (voting possible), or archived with totals only.</summary>
public enum MeetingStatus
{
    Draft = 0,
    Ready = 1,
    Archived = 2,
}

/// <summary>A proposal (for, against, abstain) or the election of people.</summary>
public enum AgendaItemKind
{
    Proposal = 0,
    Election = 1,
}

/// <summary>The majority a proposal needs (statutes art. 22.3 and 24.3).</summary>
public enum MajorityRule
{
    /// <summary>More than half of the valid votes; abstentions and blank votes do not count.</summary>
    Absolute = 0,

    /// <summary>At least two thirds of the valid votes, for example to change the statutes.</summary>
    TwoThirds = 1,
}

/// <summary>An agenda item waits, is opened live by the board, or is closed (its result is then visible).</summary>
public enum AgendaItemState
{
    Pending = 0,
    Open = 1,
    Closed = 2,
}

public enum OptionRole
{
    For = 0,
    Against = 1,
    Abstain = 2,
    Candidate = 3,
    Blank = 4,
}

/// <summary>How a vote arrived. Only totals per channel are kept, never single votes.</summary>
public enum VoteChannel
{
    Advance = 0,
    Hall = 1,
    Paper = 2,
}

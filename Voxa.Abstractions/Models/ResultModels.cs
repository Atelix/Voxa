using System.ComponentModel.DataAnnotations;
using Voxa.Persistence.Entity;

namespace Voxa.Abstractions.Models;

/// <summary>How an item ended. Elections and proposals use different values.</summary>
public enum ResultOutcome
{
    NoVotes = 0,
    Adopted = 1,
    Rejected = 2,

    /// <summary>Every seat is filled by a candidate with an absolute majority.</summary>
    Elected = 3,

    /// <summary>Not every seat is filled by an absolute majority: a second round is needed (statutes art. 22.5).</summary>
    NoMajority = 4,

    /// <summary>Candidates tie on the last seat.</summary>
    Tie = 5,
}

public sealed record ChannelCountModel
{
    public required int Advance { get; init; }

    public required int Hall { get; init; }

    public required int Paper { get; init; }

    public required int Total { get; init; }
}

public sealed record OptionResultModel
{
    public required Guid OptionId { get; init; }

    public required string Label { get; init; }

    public required OptionRole Role { get; init; }

    public required ChannelCountModel Votes { get; init; }

    public required bool Elected { get; init; }
}

/// <summary>The totals of a closed item.</summary>
public sealed record ResultModel
{
    public required Guid ItemId { get; init; }

    public required Guid MeetingId { get; init; }

    public required int Position { get; init; }

    public required string Title { get; init; }

    public required AgendaItemKind Kind { get; init; }

    public required MajorityRule Majority { get; init; }

    public required int Seats { get; init; }

    public required ChannelCountModel Ballots { get; init; }

    public required IReadOnlyList<OptionResultModel> Options { get; init; }

    /// <summary>The votes that count: for plus against, or the ballots that are not blank.</summary>
    public required int ValidVotes { get; init; }

    /// <summary>The number of votes needed: more than half, or two thirds, of <see cref="ValidVotes"/>.</summary>
    public required int VotesNeeded { get; init; }

    public required ResultOutcome Outcome { get; init; }
}

public sealed record LiveItemModel
{
    public required Guid Id { get; init; }

    public required int Position { get; init; }

    public required string Title { get; init; }

    public required AgendaItemKind Kind { get; init; }

    public required AgendaItemState State { get; init; }

    public required bool AdvanceVoting { get; init; }

    public required ChannelCountModel Ballots { get; init; }
}

/// <summary>The live meeting as the board sees it: turnout only, results only for closed items.</summary>
public sealed record LiveModel
{
    public required MeetingSummaryModel Meeting { get; init; }

    public required bool AdvanceOpenNow { get; init; }

    public required Guid? ScreenItemId { get; init; }

    public required IReadOnlyList<LiveItemModel> Items { get; init; }
}

public sealed class PaperVotesRequest
{
    /// <summary>The number of paper ballots for this item.</summary>
    [Range(0, 100000)]
    public int Ballots { get; init; }

    [Required]
    public required IReadOnlyList<PaperOptionCount> Options { get; init; }
}

public sealed class PaperOptionCount
{
    public Guid OptionId { get; init; }

    [Range(0, 100000)]
    public int Count { get; init; }
}

public sealed class ScreenRequest
{
    /// <summary>A closed item to show on the hall screen, or empty to show the open item again.</summary>
    public Guid? ItemId { get; init; }
}

/// <summary>What the hall screen shows: the open item and its count, or a result the board put on screen.</summary>
public sealed record ScreenModel
{
    public required string MeetingName { get; init; }

    public required LiveItemModel? OpenItem { get; init; }

    public required ResultModel? Result { get; init; }
}

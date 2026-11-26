using System.ComponentModel.DataAnnotations;
using Voxa.Persistence.Entity;

namespace Voxa.Abstractions.Models;

/// <summary>A meeting in the board's list.</summary>
public record MeetingSummaryModel
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required MeetingKind Kind { get; init; }

    public required DateTime StartsAt { get; init; }

    public required string? Location { get; init; }

    public required MeetingStatus Status { get; init; }

    public required bool AdvanceVoting { get; init; }

    public required DateTime? AdvanceOpensAt { get; init; }

    public required int ItemCount { get; init; }

    public required int VoterCount { get; init; }

    public required int CodesIssued { get; init; }

    /// <summary>Codes that voted at least once; known after archiving, or live while the codes exist.</summary>
    public required int CodesUsed { get; init; }

    public required DateTime? ArchivedAt { get; init; }
}

/// <summary>A meeting with its agenda, for the board.</summary>
public sealed record MeetingModel : MeetingSummaryModel
{
    /// <summary>The earliest moment advance voting may open: 30 days before the meeting (statutes art. 20.8).</summary>
    public required DateTime EarliestAdvanceOpensAt { get; init; }

    public required int? ScreenItemId { get; init; }

    public required IReadOnlyList<AgendaItemModel> Items { get; init; }
}

public sealed record AgendaItemModel
{
    public required int Id { get; init; }

    public required int Position { get; init; }

    public required string Title { get; init; }

    public required string? Description { get; init; }

    public required AgendaItemKind Kind { get; init; }

    public required MajorityRule Majority { get; init; }

    public required int Seats { get; init; }

    public required AgendaItemState State { get; init; }

    public required bool AdvanceVoting { get; init; }

    public required IReadOnlyList<OptionModel> Options { get; init; }

    /// <summary>Ballots cast so far, over every channel. Only the count; never what was voted.</summary>
    public required int Ballots { get; init; }

    /// <summary>The item can no longer be edited: it has votes or has been opened.</summary>
    public required bool Locked { get; init; }
}

public sealed record OptionModel
{
    public required int Id { get; init; }

    public required string Label { get; init; }

    public required string? Note { get; init; }

    public required OptionRole Role { get; init; }
}

public sealed class MeetingRequest
{
    public const int NameMaxLength = 200;

    [Required]
    [StringLength(NameMaxLength)]
    public required string Name { get; init; }

    public MeetingKind Kind { get; init; }

    public required DateTime StartsAt { get; init; }

    [StringLength(300)]
    public string? Location { get; init; }

    public bool AdvanceVoting { get; init; }

    /// <summary>When advance voting opens; empty means the earliest moment the statutes allow.</summary>
    public DateTime? AdvanceOpensAt { get; init; }
}

public sealed class AgendaItemRequest
{
    public const int TitleMaxLength = 300;

    [Required]
    [StringLength(TitleMaxLength)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    public AgendaItemKind Kind { get; init; }

    public MajorityRule Majority { get; init; }

    /// <summary>Election only: the number of seats.</summary>
    [Range(0, 50)]
    public int Seats { get; init; }

    public bool AdvanceVoting { get; init; } = true;

    /// <summary>Election only: the candidates, in ballot order.</summary>
    public IReadOnlyList<CandidateRequest> Candidates { get; init; } = [];
}

public sealed class CandidateRequest
{
    [Required]
    [StringLength(200)]
    public required string Label { get; init; }

    [StringLength(300)]
    public string? Note { get; init; }
}

public sealed class ItemOrderRequest
{
    [Required]
    public required IReadOnlyList<int> ItemIds { get; init; }
}

public sealed class WipeRequest
{
    /// <summary>Must be the word WISSEN, so nobody wipes the data by accident.</summary>
    [Required]
    public required string Confirm { get; init; }
}

/// <summary>Everything worth keeping after the season: per meeting, the totals of every item.</summary>
public sealed record ExportModel
{
    public required DateTime ExportedAt { get; init; }

    public required IReadOnlyList<MeetingExportModel> Meetings { get; init; }
}

public sealed record MeetingExportModel
{
    public required MeetingSummaryModel Meeting { get; init; }

    public required IReadOnlyList<ResultModel> Results { get; init; }
}

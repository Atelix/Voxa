using System.ComponentModel.DataAnnotations;
using Voxa.Persistence.Entity;

namespace Voxa.Abstractions.Models;

/// <summary>Where the voting period is for a meeting.</summary>
public enum VotingPhase
{
    /// <summary>Codes are valid, but nothing can be voted yet.</summary>
    NotYetOpen = 0,
    Advance = 1,
    Meeting = 2,
    Ended = 3,
}

/// <summary>An item from the voter's point of view.</summary>
public enum BallotItemStatus
{
    Pending = 0,
    Open = 1,
    Voted = 2,
    Closed = 3,
}

public sealed class BallotCodeRequest
{
    [Required]
    [StringLength(32)]
    public required string Code { get; init; }
}

public sealed class VoteRequest
{
    [Required]
    [StringLength(32)]
    public required string Code { get; init; }

    public Guid ItemId { get; init; }

    /// <summary>The chosen option, or for an election the chosen candidates or only the blank option.</summary>
    [Required]
    public required IReadOnlyList<Guid> OptionIds { get; init; }
}

public sealed record BallotSessionModel
{
    public required string MeetingName { get; init; }

    public required DateTime StartsAt { get; init; }

    public required string? Location { get; init; }

    public required DateTime? AdvanceOpensAt { get; init; }

    public required VotingPhase Phase { get; init; }

    public required IReadOnlyList<BallotItemModel> Items { get; init; }
}

public sealed record BallotItemModel
{
    public required Guid Id { get; init; }

    public required int Position { get; init; }

    public required string Title { get; init; }

    public required string? Description { get; init; }

    public required AgendaItemKind Kind { get; init; }

    public required int Seats { get; init; }

    public required BallotItemStatus Status { get; init; }

    public required IReadOnlyList<OptionModel> Options { get; init; }
}

public sealed class SignInRequest
{
    [Required]
    [StringLength(200)]
    public required string Password { get; init; }
}

public sealed record AdminSessionModel
{
    public required bool SignedIn { get; init; }

    /// <summary>Mail goes to the in-memory test mailbox instead of Mailgun; demo data may be loaded.</summary>
    public required bool TrialMode { get; init; }
}

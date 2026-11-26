using System.ComponentModel.DataAnnotations;

namespace Voxa.Abstractions.Models;

public sealed record VoterModel
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string? Email { get; init; }

    public required string? GuardianName { get; init; }

    public required bool CodeSent { get; init; }
}

public sealed record VoterListModel
{
    public required int Total { get; init; }

    public required int ViaGuardian { get; init; }

    public required int WithoutEmail { get; init; }

    public required int CodesSent { get; init; }

    public required IReadOnlyList<VoterModel> Voters { get; init; }
}

public sealed class VoterImportRequest
{
    /// <summary>The member list as CSV text (comma or semicolon), with a header row. See docs/voxa.md.</summary>
    [Required]
    [StringLength(2_000_000)]
    public required string Csv { get; init; }
}

/// <summary>What an import did with the voter list.</summary>
public enum VoterImportMode
{
    /// <summary>No code had been sent yet, so the list was replaced.</summary>
    Replaced = 0,

    /// <summary>Codes had been sent, so only members who were not on the list yet were added. Nobody was removed.</summary>
    Added = 1,
}

public sealed record VoterImportResultModel
{
    public required VoterImportMode Mode { get; init; }

    /// <summary>Voters put on the list: the whole list when replaced, the new voters when added.</summary>
    public required int Imported { get; init; }

    /// <summary>When adding: members who were already on the list (by name) and were left as they were.</summary>
    public required int AlreadyListed { get; init; }

    public required int ViaGuardian { get; init; }

    public required int WithoutEmail { get; init; }

    /// <summary>Rows that were left out, such as suspended members, with the reason.</summary>
    public required IReadOnlyList<string> Skipped { get; init; }
}

public sealed class VoterRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    [StringLength(320)]
    [EmailAddress]
    public string? Email { get; init; }

    [StringLength(200)]
    public string? GuardianName { get; init; }
}

public sealed record SendCodesResultModel
{
    public required int Sent { get; init; }

    public required int Failed { get; init; }

    /// <summary>Voters without an e-mail address; they need a paper code.</summary>
    public required int WithoutEmail { get; init; }
}

public sealed class PaperCodesRequest
{
    [Range(1, 500)]
    public int Count { get; init; }
}

/// <summary>Fresh codes for paper slips. They are shown once and stored only as hashes.</summary>
public sealed record PaperCodesModel
{
    public required IReadOnlyList<string> Codes { get; init; }
}

public sealed class TestMailRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string Email { get; init; }
}

public sealed record OutboxMailModel
{
    public required string To { get; init; }

    public required string Subject { get; init; }

    public required string Text { get; init; }

    public required DateTime SentAt { get; init; }
}

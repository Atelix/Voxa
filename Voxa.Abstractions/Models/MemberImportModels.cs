using System.ComponentModel.DataAnnotations;
using Voxa.Persistence.Entity;

namespace Voxa.Abstractions.Models;

/// <summary>
/// The club's current members, sent by a member administration or an automation (such as n8n) to the import API. Send
/// every member, also those without a vote: Voxa applies the club's rules for suspension and minors itself.
/// </summary>
public sealed class MemberImportRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10_000)]
    public required IReadOnlyList<MemberImportItem> Members { get; init; }
}

/// <summary>One member of the club.</summary>
public sealed class MemberImportItem
{
    /// <summary>The member's full name, as the voter list and the code mail show it, for example "Jan de Vries".</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>The member's own address. Members of age without one get a paper code.</summary>
    [StringLength(320)]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>Tells minors apart (<c>yyyy-MM-dd</c>). Without it the member counts as of age.</summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>A suspended member has no vote and is left out of the voter list.</summary>
    public bool Suspended { get; init; }

    /// <summary>
    /// The member's parents or guardians, primary contact first. When a minor votes through a parent or guardian, the
    /// code goes to the first one with an address, and the mail greets them by name.
    /// </summary>
    [MaxLength(10)]
    public IReadOnlyList<GuardianImportItem>? Guardians { get; init; }
}

/// <summary>A parent or guardian of a member.</summary>
public sealed class GuardianImportItem
{
    [StringLength(200)]
    public string? Name { get; init; }

    [StringLength(320)]
    [EmailAddress]
    public string? Email { get; init; }
}

/// <summary>A meeting the import API can send the member list to: every meeting that is not archived.</summary>
public sealed record ImportMeetingModel
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required DateTime StartsAt { get; init; }

    public required MeetingStatus Status { get; init; }

    public required int VoterCount { get; init; }

    /// <summary>
    /// <see cref="VoterImportMode.Replaced"/> while no code has been sent: an import replaces the voter list.
    /// <see cref="VoterImportMode.Added"/> after that: an import only adds members who are not on the list yet.
    /// </summary>
    public required VoterImportMode ImportMode { get; init; }
}

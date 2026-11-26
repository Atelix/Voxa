namespace Voxa.Abstractions.Services;

/// <summary>A member as a member administration delivers it (CSV or the import API), before the club's voting rules are applied.</summary>
public sealed record ImportedMember
{
    public required string Name { get; init; }

    public string? Email { get; init; }

    public DateOnly? BirthDate { get; init; }

    public string? GuardianName { get; init; }

    public string? GuardianEmail { get; init; }

    /// <summary>Suspended members have no vote (for example statutes art. 20.3).</summary>
    public bool Suspended { get; init; }
}

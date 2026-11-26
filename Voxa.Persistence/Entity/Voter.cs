namespace Voxa.Persistence.Entity;

/// <summary>
/// A member entitled to vote at a meeting, imported from the member list. Used only to send codes; it carries no code
/// and is deleted when the meeting is archived.
/// </summary>
public class Voter
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>Where the code goes: the member's address, or the parent's or guardian's for a minor.</summary>
    public string? Email { get; set; }

    /// <summary>The name of the parent or guardian who receives the code for a minor.</summary>
    public string? GuardianName { get; set; }

    public bool CodeSent { get; set; }
}

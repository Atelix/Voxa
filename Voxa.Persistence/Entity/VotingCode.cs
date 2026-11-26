namespace Voxa.Persistence.Entity;

/// <summary>
/// A voting code, stored only as a keyed hash. It names no member: the link between code and member exists only in
/// the e-mail that was sent.
/// </summary>
public class VotingCode
{
    public Guid Id { get; set; }

    public int MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public required string Hash { get; set; }

    public ICollection<CodeUse> Uses { get; set; } = [];
}

/// <summary>The "used" flag of a code on one agenda item: it voted there. It does not say what it voted.</summary>
public class CodeUse
{
    public Guid VotingCodeId { get; set; }

    public VotingCode VotingCode { get; set; } = null!;

    public int AgendaItemId { get; set; }
}

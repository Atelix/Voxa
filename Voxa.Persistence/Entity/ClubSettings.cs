namespace Voxa.Persistence.Entity;

/// <summary>How minors take part in a vote.</summary>
public enum MinorVoting
{
    /// <summary>The code goes to the parent or guardian, who votes on the minor's behalf.</summary>
    Guardian = 0,

    /// <summary>Minors receive and use their own code.</summary>
    Self = 1,

    /// <summary>Minors have no vote and are left out of the voter list.</summary>
    None = 2,
}

/// <summary>Where the voter list comes from.</summary>
public enum MemberSourceKind
{
    Csv = 0,
    Keeva = 1,
}

/// <summary>
/// The club this installation serves: its name, brand and voting rules. Voxa runs one installation per club, so there
/// is a single row (id 1). Secrets such as the Mailgun and Keeva keys are not here but in the server's settings.
/// </summary>
public class ClubSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public required string ClubName { get; set; }

    /// <summary>The address members can write to, shown when a code did not arrive.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The club's language as a two-letter code (Voxa.Abstractions.Languages): mails use it unless the meeting has its
    /// own, and the app shows it to people whose device asks for a language Voxa does not have.
    /// </summary>
    public string Language { get; set; } = "nl";

    public required string PrimaryColor { get; set; }

    public required string SecondaryColor { get; set; }

    public required string HeadingFont { get; set; }

    public required string BodyFont { get; set; }

    /// <summary>The logo as a data URL (PNG, JPEG, SVG or WebP), so the app needs no other host for it.</summary>
    public string? LogoDataUrl { get; set; }

    public required string MailFromName { get; set; }

    /// <summary>The sending address, on the domain the mail provider is set up for.</summary>
    public string? MailFromAddress { get; set; }

    public string? MailReplyTo { get; set; }

    /// <summary>How many days before the meeting advance voting may open at the earliest (statutes, for example 30).</summary>
    public int AdvanceVotingMaxDays { get; set; }

    /// <summary>The statutes article that allows advance voting, shown next to the setting, for example "art. 20.8".</summary>
    public string? AdvanceVotingArticle { get; set; }

    public MinorVoting MinorVoting { get; set; }

    /// <summary>The age from which a member votes for themselves.</summary>
    public int AdultAge { get; set; }

    public MemberSourceKind MemberSource { get; set; }

    /// <summary>The club's Keeva address, for example https://sittardcondors.keeva.app. The API key is a server secret.</summary>
    public string? KeevaUrl { get; set; }
}

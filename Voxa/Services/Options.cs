namespace Voxa.Services;

/// <summary>How mail leaves the server. The sender name and address are club settings.</summary>
public sealed class MailOptions
{
    public const string Section = "Mail";

    /// <summary><c>Outbox</c> (trial: mail stays in memory and never leaves the server) or <c>Mailgun</c>.</summary>
    public string Mode { get; set; } = "Outbox";

    /// <summary>When set, every mail goes to this address instead of the voter: for a trial with real mail.</summary>
    public string? RedirectTo { get; set; }

    public MailgunOptions Mailgun { get; set; } = new();

    public bool IsMailgun => string.Equals(Mode, "Mailgun", StringComparison.OrdinalIgnoreCase);
}

public sealed class MailgunOptions
{
    public string BaseUrl { get; set; } = "https://api.eu.mailgun.net/v3/";

    public string? Domain { get; set; }

    public string? ApiKey { get; set; }
}

/// <summary>The club's Keeva API key, with the scopes Members, MembersContacts and MembersPersonalData.</summary>
public sealed class KeevaOptions
{
    public const string Section = "Keeva";

    public string? ApiKey { get; set; }
}

public sealed class AppOptions
{
    public const string Section = "App";

    /// <summary>The voting page's address, used in mails, for example https://stem.sittardcondors.nl.</summary>
    public string PublicUrl { get; set; } = "http://localhost:3000";
}

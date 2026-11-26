namespace Voxa.Abstractions.Services;

/// <summary>A mail to send. <paramref name="From"/> is a full address header, such as <c>Club &lt;noreply@club.nl&gt;</c>.</summary>
public sealed record OutgoingMail(string From, string? ReplyTo, string To, string Subject, string Text, string Html);

/// <summary>Sends mail through Mailgun, or into the in-memory test mailbox in the trial setup.</summary>
public interface IMailSender
{
    /// <summary>True when mail goes to the test mailbox and never leaves the server.</summary>
    bool IsTrial { get; }

    /// <returns>False when the provider refused the message.</returns>
    Task<bool> SendAsync(OutgoingMail mail, CancellationToken cancellationToken);
}

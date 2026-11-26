using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>The trial setup: demo data and the test mailbox. Only available while mail goes to the test mailbox.</summary>
public interface ITrialLogic
{
    /// <summary>Creates a demo meeting with test voters and mails their codes to the test mailbox.</summary>
    /// <exception cref="DomainForbiddenException">Real mail is configured.</exception>
    Task<MeetingModel> CreateDemoAsync(CancellationToken cancellationToken);

    /// <exception cref="DomainForbiddenException">Real mail is configured.</exception>
    IReadOnlyList<OutboxMailModel> Outbox();
}

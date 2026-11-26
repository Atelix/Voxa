using System.Collections.Concurrent;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>The trial setup's mailbox: mail stays in the server's memory, so no member ever receives a test code.</summary>
public sealed class OutboxMailSender(TimeProvider time) : IMailSender, IOutbox
{
    private const int Capacity = 500;
    private readonly ConcurrentQueue<OutboxMailModel> _messages = new();

    public bool IsTrial => true;

    public Task<bool> SendAsync(OutgoingMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);
        _messages.Enqueue(new OutboxMailModel { To = mail.To, Subject = mail.Subject, Text = mail.Text, SentAt = time.GetUtcNow().UtcDateTime });
        while (_messages.Count > Capacity && _messages.TryDequeue(out _))
        {
        }

        return Task.FromResult(true);
    }

    public IReadOnlyList<OutboxMailModel> Messages() => [.. _messages.Reverse()];
}

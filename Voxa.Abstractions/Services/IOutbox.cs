using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Services;

/// <summary>The test mailbox of the trial setup, kept in memory only.</summary>
public interface IOutbox
{
    IReadOnlyList<OutboxMailModel> Messages();
}

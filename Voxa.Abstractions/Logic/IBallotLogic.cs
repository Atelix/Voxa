using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>Voting with a code. Failed attempts are limited per device and per network address.</summary>
public interface IBallotLogic
{
    /// <exception cref="ObjectNotFoundException">The code is unknown.</exception>
    /// <exception cref="DomainRateLimitedException">Too many wrong codes from this client.</exception>
    Task<BallotSessionModel> SessionAsync(BallotCodeRequest request, CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">The code or the item is unknown.</exception>
    /// <exception cref="DomainRateLimitedException">Too many wrong codes from this client.</exception>
    /// <exception cref="DomainForbiddenException">The item is not open for voting.</exception>
    /// <exception cref="DomainConflictException">This code already voted on this item.</exception>
    /// <exception cref="DomainValidationException">The choice is not valid for this item.</exception>
    Task VoteAsync(VoteRequest request, CancellationToken cancellationToken);
}

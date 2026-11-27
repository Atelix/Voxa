using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>Turnout while voting runs, results once an item is closed, paper votes and the hall screen.</summary>
public interface IResultLogic
{
    Task<LiveModel> LiveAsync(Guid meetingId, CancellationToken cancellationToken);

    /// <exception cref="DomainForbiddenException">The item is not closed yet, so its result stays hidden.</exception>
    Task<ResultModel> ResultAsync(Guid itemId, CancellationToken cancellationToken);

    /// <summary>Sets the paper votes of a closed item, replacing an earlier count.</summary>
    /// <exception cref="DomainValidationException">The counts do not add up, or name an option of another item.</exception>
    Task<ResultModel> SetPaperVotesAsync(Guid itemId, PaperVotesRequest request, CancellationToken cancellationToken);

    /// <exception cref="DomainValidationException">The item is not a closed item of this meeting.</exception>
    Task SetScreenAsync(Guid meetingId, ScreenRequest request, CancellationToken cancellationToken);

    /// <summary>Public: what the hall screen shows.</summary>
    Task<ScreenModel> ScreenAsync(Guid meetingId, CancellationToken cancellationToken);
}

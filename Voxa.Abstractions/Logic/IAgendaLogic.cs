using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>The agenda items of a meeting: edit them before voting, open and close them live.</summary>
public interface IAgendaLogic
{
    /// <exception cref="ObjectNotFoundException">No meeting has this id.</exception>
    /// <exception cref="DomainValidationException">The item breaks a rule, such as an election without candidates.</exception>
    /// <exception cref="DomainConflictException">The meeting is archived.</exception>
    Task<AgendaItemModel> AddAsync(int meetingId, AgendaItemRequest request, CancellationToken cancellationToken);

    /// <exception cref="DomainConflictException">The item has votes or has been opened.</exception>
    Task<AgendaItemModel> UpdateAsync(int itemId, AgendaItemRequest request, CancellationToken cancellationToken);

    /// <exception cref="DomainConflictException">The item has votes or has been opened.</exception>
    Task DeleteAsync(int itemId, CancellationToken cancellationToken);

    /// <exception cref="DomainValidationException">The list does not name every item of the meeting exactly once.</exception>
    Task<IReadOnlyList<AgendaItemModel>> ReorderAsync(int meetingId, ItemOrderRequest request, CancellationToken cancellationToken);

    /// <summary>Opens the item for voting in the hall.</summary>
    /// <exception cref="DomainConflictException">No codes have been sent yet, or the item is closed.</exception>
    Task<AgendaItemModel> OpenAsync(int itemId, CancellationToken cancellationToken);

    /// <summary>Closes the item: no more votes, and its result becomes visible.</summary>
    Task<AgendaItemModel> CloseAsync(int itemId, CancellationToken cancellationToken);
}

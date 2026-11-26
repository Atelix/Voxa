using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>Business rules for items. Controllers call this; it returns models, never entities.</summary>
public interface IItemLogic
{
    Task<IReadOnlyList<ItemModel>> ListAsync(CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">No item has this id.</exception>
    Task<ItemModel> GetAsync(int id, CancellationToken cancellationToken);

    /// <exception cref="DomainValidationException">The request breaks a rule, such as a blank name.</exception>
    /// <exception cref="DomainConflictException">Another item already has this name.</exception>
    Task<ItemModel> CreateAsync(ItemRequest request, CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">No item has this id.</exception>
    /// <exception cref="DomainValidationException">The request breaks a rule, such as a blank name.</exception>
    /// <exception cref="DomainConflictException">Another item already has this name.</exception>
    Task<ItemModel> UpdateAsync(int id, ItemRequest request, CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">No item has this id.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

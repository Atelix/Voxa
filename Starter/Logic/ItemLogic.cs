using Microsoft.EntityFrameworkCore;
using Starter.Abstractions;
using Starter.Abstractions.Logic;
using Starter.Abstractions.Models;
using Starter.Persistence.Entity;

namespace Starter.Logic;

public sealed class ItemLogic(StarterContext context, TimeProvider time) : IItemLogic
{
    public async Task<IReadOnlyList<ItemModel>> ListAsync(CancellationToken cancellationToken) =>
        await context.Items
                     .AsNoTracking()
                     .OrderBy(static i => i.Done)
                     .ThenBy(static i => i.Name)
                     .Select(static i => new ItemModel
                     {
                         Id = i.Id,
                         Name = i.Name,
                         Description = i.Description,
                         Done = i.Done,
                         CreatedAt = i.CreatedAt,
                         UpdatedAt = i.UpdatedAt,
                     })
                     .ToListAsync(cancellationToken);

    public async Task<ItemModel> GetAsync(int id, CancellationToken cancellationToken) =>
        ToModel(await FindAsync(id, cancellationToken));

    public async Task<ItemModel> CreateAsync(ItemRequest request, CancellationToken cancellationToken)
    {
        var name = ValidName(request);
        await EnsureUniqueNameAsync(name, null, cancellationToken);

        var now = time.GetUtcNow().UtcDateTime;
        var item = new Item
        {
            Name = name,
            Description = Clean(request.Description),
            Done = request.Done,
            CreatedAt = now,
            UpdatedAt = now,
        };
        context.Items.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return ToModel(item);
    }

    public async Task<ItemModel> UpdateAsync(int id, ItemRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        var name = ValidName(request);
        await EnsureUniqueNameAsync(name, id, cancellationToken);

        item.Name = name;
        item.Description = Clean(request.Description);
        item.Done = request.Done;
        item.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
        return ToModel(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        context.Items.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Item> FindAsync(int id, CancellationToken cancellationToken) =>
        await context.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
        ?? throw new ObjectNotFoundException(nameof(Item), id);

    private async Task EnsureUniqueNameAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        var taken = await context.Items.AnyAsync(i => i.Name == name && i.Id != exceptId, cancellationToken);
        if (taken)
        {
            throw new DomainConflictException($"An item named '{name}' already exists.");
        }
    }

    private static string ValidName(ItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new DomainValidationException("An item needs a name.");
        }

        return name.Length > ItemRequest.NameMaxLength
            ? throw new DomainValidationException($"An item name has at most {ItemRequest.NameMaxLength} characters.")
            : name;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ItemModel ToModel(Item item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Done = item.Done,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };
}

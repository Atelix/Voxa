using Microsoft.EntityFrameworkCore;
using Starter.Abstractions;
using Starter.Abstractions.Models;
using Starter.Logic;

namespace Starter.WebApi.PostgresTests;

[Collection(PostgresCollection.Name)]
public sealed class MigrationTests(PostgresFixture database)
{
    [Fact]
    public async Task EveryMigrationIsApplied()
    {
        await using var context = database.CreateContext();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task TheModelHasNoChangesWithoutAMigration()
    {
        await using var context = database.CreateContext();

        Assert.False(context.Database.HasPendingModelChanges(), "The model changed without a migration: run dotnet ef migrations add.");
    }

    [Fact]
    public async Task TheUniqueNameIndexHolds()
    {
        await using var context = database.CreateContext();
        var logic = new ItemLogic(context, TimeProvider.System);
        var name = $"Unique {Guid.NewGuid()}";
        await logic.CreateAsync(new ItemRequest { Name = name }, CancellationToken.None);

        await Assert.ThrowsAsync<DomainConflictException>(() => logic.CreateAsync(new ItemRequest { Name = name }, CancellationToken.None));
    }
}

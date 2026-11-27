using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;
using Voxa.Logic;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.WebApi.PostgresTests;

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
    public async Task ACodeHashIsUnique()
    {
        await using var context = database.CreateContext();
        var meeting = new Meeting { Name = "ALV", StartsAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        _ = context.Meetings.Add(meeting);
        _ = await context.SaveChangesAsync();
        var hash = Guid.NewGuid().ToString("N");
        _ = context.VotingCodes.Add(new VotingCode { Id = Guid.NewGuid(), MeetingId = meeting.Id, Hash = hash });
        _ = context.VotingCodes.Add(new VotingCode { Id = Guid.NewGuid(), MeetingId = meeting.Id, Hash = hash });

        _ = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task VotesAtTheSameMomentAreAllCountedAndACodeOnlyOnce()
    {
        var codes = new CodeService(Options.Create(new CodeOptions { Pepper = "pg" }));
        Guid itemId, optionId;
        var plain = Enumerable.Range(0, 20).Select(_ => codes.Generate()).ToList();
        await using (var setup = database.CreateContext())
        {
            var meeting = new Meeting { Name = "ALV", StartsAt = DateTime.UtcNow.AddHours(1), CreatedAt = DateTime.UtcNow, Status = MeetingStatus.Ready };
            var item = new AgendaItem { Title = "Begroting", State = AgendaItemState.Open, Meeting = meeting };
            item.Options.Add(new ItemOption { Label = "Voor", Role = OptionRole.For, Position = 1 });
            _ = setup.AgendaItems.Add(item);
            foreach (var code in plain)
            {
                _ = setup.VotingCodes.Add(new VotingCode { Id = Guid.NewGuid(), Meeting = meeting, Hash = codes.Hash(codes.Normalize(code)!) });
            }

            _ = await setup.SaveChangesAsync();
            itemId = item.Id;
            optionId = item.Options.Single().Id;
        }

        // Every code votes twice at once; each code must count once and every first vote must count.
        var attempts = plain.Concat(plain).Select(async code =>
        {
            await using var context = database.CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var logic = new BallotLogic(context, codes, new AttemptLimiter(cache, TimeProvider.System), new Client(code), TimeProvider.System);
            try
            {
                await logic.VoteAsync(new VoteRequest { Code = code, ItemId = itemId, OptionIds = [optionId] }, CancellationToken.None);
                return true;
            }
            catch (DomainConflictException)
            {
                return false;
            }
        });
        var outcomes = await Task.WhenAll(attempts);

        await using var check = database.CreateContext();
        Assert.Equal(plain.Count, outcomes.Count(static ok => ok));
        Assert.Equal(plain.Count, await check.BallotTallies.Where(t => t.AgendaItemId == itemId).SumAsync(static t => t.Count));
        Assert.Equal(plain.Count, await check.OptionTallies.Where(t => t.ItemOptionId == optionId).SumAsync(static t => t.Count));
        Assert.Equal(plain.Count, await check.CodeUses.CountAsync(u => u.AgendaItemId == itemId));
    }

    private sealed class Client(string device) : IClientIdentity
    {
        public string Address => "192.0.2.10";

        public string Device => "device:" + device;
    }
}

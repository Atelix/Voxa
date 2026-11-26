using Microsoft.Extensions.Time.Testing;
using Starter.Abstractions;
using Starter.Abstractions.Models;
using Starter.Logic;

namespace Starter.WebApi.UnitTests.Logic;

public sealed class ItemLogicTests : IDisposable
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);
    private readonly StarterContext _context = TestDatabase.Create();
    private readonly FakeTimeProvider _time = new(s_now);
    private readonly ItemLogic _logic;

    public ItemLogicTests() => _logic = new ItemLogic(_context, _time);

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateTrimsTheNameAndStampsTheTime()
    {
        var item = await _logic.CreateAsync(new ItemRequest { Name = "  Write docs  ", Description = " " }, CancellationToken.None);

        Assert.Equal("Write docs", item.Name);
        Assert.Null(item.Description);
        Assert.Equal(s_now.UtcDateTime, item.CreatedAt);
        Assert.Equal(s_now.UtcDateTime, item.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRefusesABlankName(string name) =>
        await Assert.ThrowsAsync<DomainValidationException>(() => _logic.CreateAsync(new ItemRequest { Name = name }, CancellationToken.None));

    [Fact]
    public async Task CreateRefusesADuplicateName()
    {
        await _logic.CreateAsync(new ItemRequest { Name = "Release" }, CancellationToken.None);

        await Assert.ThrowsAsync<DomainConflictException>(() => _logic.CreateAsync(new ItemRequest { Name = "Release" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateKeepsCreatedAtAndMovesUpdatedAt()
    {
        var created = await _logic.CreateAsync(new ItemRequest { Name = "Plan" }, CancellationToken.None);
        _time.Advance(TimeSpan.FromHours(1));

        var updated = await _logic.UpdateAsync(created.Id, new ItemRequest { Name = "Plan", Done = true }, CancellationToken.None);

        Assert.True(updated.Done);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(s_now.AddHours(1).UtcDateTime, updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnName()
    {
        var created = await _logic.CreateAsync(new ItemRequest { Name = "Same" }, CancellationToken.None);

        var updated = await _logic.UpdateAsync(created.Id, new ItemRequest { Name = "Same", Description = "More" }, CancellationToken.None);

        Assert.Equal("More", updated.Description);
    }

    [Fact]
    public async Task MissingItemsAreNotFound()
    {
        var exception = await Assert.ThrowsAsync<ObjectNotFoundException>(() => _logic.GetAsync(42, CancellationToken.None));

        Assert.Equal(42, exception.Identifier);
        await Assert.ThrowsAsync<ObjectNotFoundException>(() => _logic.DeleteAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task ListShowsOpenItemsFirstThenByName()
    {
        await _logic.CreateAsync(new ItemRequest { Name = "b", Done = true }, CancellationToken.None);
        await _logic.CreateAsync(new ItemRequest { Name = "c" }, CancellationToken.None);
        await _logic.CreateAsync(new ItemRequest { Name = "a" }, CancellationToken.None);

        var items = await _logic.ListAsync(CancellationToken.None);

        Assert.Equal(["a", "c", "b"], items.Select(static i => i.Name));
    }
}

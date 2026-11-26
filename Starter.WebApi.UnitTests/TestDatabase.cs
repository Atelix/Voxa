using Microsoft.EntityFrameworkCore;

namespace Starter.WebApi.UnitTests;

/// <summary>
/// A fresh EF in-memory database per test. It ignores PostgreSQL rules (unique indexes, case, time zones); tests that
/// depend on those belong in Starter.WebApi.PostgresTests.
/// </summary>
internal static class TestDatabase
{
    public static StarterContext Create() =>
        new(new DbContextOptionsBuilder<StarterContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

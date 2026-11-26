using Microsoft.EntityFrameworkCore;

namespace Voxa.WebApi.UnitTests;

/// <summary>
/// A fresh EF in-memory database per test. It ignores PostgreSQL rules (unique indexes, case, time zones); tests that
/// depend on those belong in Voxa.WebApi.PostgresTests.
/// </summary>
internal static class TestDatabase
{
    public static VoxaContext Create() =>
        new(new DbContextOptionsBuilder<VoxaContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

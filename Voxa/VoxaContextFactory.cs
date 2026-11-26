using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Voxa;

/// <summary>
/// Builds the context outside the web host: for the <c>dotnet ef</c> tools and for tests that inspect the model.
/// The running API configures the context in <c>Program.cs</c> from its configuration instead.
/// </summary>
public class VoxaContextFactory : IDesignTimeDbContextFactory<VoxaContext>
{
    /// <summary>The local database used in Development when no <c>ConnectionString</c> is configured.</summary>
    public const string DevelopmentConnectionString = "Host=localhost;Port=5432;Database=Voxa;Username=root;Password=root";

    public VoxaContext CreateDbContext(string[] args) => Create(Environment.GetEnvironmentVariable("ConnectionString"));

    public static VoxaContext Create(string? connectionString) =>
        new(new DbContextOptionsBuilder<VoxaContext>().UseNpgsql(Configure(connectionString)).Options);

    /// <summary>The configured connection string, or the local development database when none is set.</summary>
    public static string Configure(string? connectionString) =>
        string.IsNullOrWhiteSpace(connectionString) ? DevelopmentConnectionString : connectionString;
}

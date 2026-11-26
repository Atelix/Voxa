using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Voxa.WebApi.UnitTests.Api;

/// <summary>Hosts the whole API in memory, in Development (no migrations), on an EF in-memory database.</summary>
public sealed class TestApi : WebApplicationFactory<Program>
{
    public const string Password = "test-board-password";
    public const string ImportKey = "test-import-key-0123456789abcdef0123456789";

    private readonly string _database = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.UseSetting("APP_VERSION", "1.2.3");
        builder.UseSetting("Admin:Password", Password);
        builder.UseSetting("Import:ApiKey", ImportKey);
        builder.ConfigureServices(services =>
        {
            // Drop the Npgsql registration from Program.cs, or EF sees two database providers.
            services.RemoveAll<DbContextOptions<VoxaContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VoxaContext>>();
            services.AddDbContext<VoxaContext>(options => options.UseInMemoryDatabase(_database));
        });
    }
}

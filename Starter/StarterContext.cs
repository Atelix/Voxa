using Microsoft.EntityFrameworkCore;
using Starter.Persistence.Entity;

namespace Starter;

public class StarterContext(DbContextOptions<StarterContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DateTime>()
                            .HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Item>(static item =>
        {
            item.Property(static i => i.Name).HasMaxLength(200);
            item.Property(static i => i.Description).HasMaxLength(2000);
            item.HasIndex(static i => i.Name).IsUnique();
        });
    }
}

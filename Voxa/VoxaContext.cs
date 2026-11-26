using Microsoft.EntityFrameworkCore;
using Voxa.Persistence.Entity;

namespace Voxa;

public class VoxaContext(DbContextOptions<VoxaContext> options) : DbContext(options)
{
    public DbSet<Meeting> Meetings { get; set; } = null!;

    public DbSet<AgendaItem> AgendaItems { get; set; } = null!;

    public DbSet<ItemOption> ItemOptions { get; set; } = null!;

    public DbSet<BallotTally> BallotTallies { get; set; } = null!;

    public DbSet<OptionTally> OptionTallies { get; set; } = null!;

    public DbSet<VotingCode> VotingCodes { get; set; } = null!;

    public DbSet<CodeUse> CodeUses { get; set; } = null!;

    public DbSet<Voter> Voters { get; set; } = null!;

    public DbSet<ClubSettings> ClubSettings { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DateTime>()
                            .HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Meeting>(static meeting =>
        {
            meeting.Property(static m => m.Name).HasMaxLength(200);
            meeting.Property(static m => m.Location).HasMaxLength(300);
            meeting.Property(static m => m.Language).HasMaxLength(8);
            meeting.HasMany(static m => m.Items).WithOne(static i => i.Meeting).HasForeignKey(static i => i.MeetingId).OnDelete(DeleteBehavior.Cascade);
            meeting.HasMany(static m => m.Voters).WithOne(static v => v.Meeting).HasForeignKey(static v => v.MeetingId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AgendaItem>(static item =>
        {
            item.Property(static i => i.Title).HasMaxLength(300);
            item.Property(static i => i.Description).HasMaxLength(4000);
            item.HasIndex(static i => new { i.MeetingId, i.Position });
            item.HasMany(static i => i.Options).WithOne(static o => o.AgendaItem).HasForeignKey(static o => o.AgendaItemId).OnDelete(DeleteBehavior.Cascade);
            item.HasMany(static i => i.Ballots).WithOne(static b => b.AgendaItem).HasForeignKey(static b => b.AgendaItemId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ItemOption>(static option =>
        {
            option.Property(static o => o.Label).HasMaxLength(200);
            option.Property(static o => o.Note).HasMaxLength(300);
            option.HasMany(static o => o.Tallies).WithOne(static t => t.ItemOption).HasForeignKey(static t => t.ItemOptionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<BallotTally>(static tally =>
        {
            tally.HasKey(static t => new { t.AgendaItemId, t.Channel });
            tally.Property(static t => t.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<OptionTally>(static tally =>
        {
            tally.HasKey(static t => new { t.ItemOptionId, t.Channel });
            tally.Property(static t => t.Version).IsConcurrencyToken();
        });
        modelBuilder.Entity<VotingCode>(static code =>
        {
            code.Property(static c => c.Hash).HasMaxLength(64);
            code.HasIndex(static c => c.Hash).IsUnique();
            code.HasOne(static c => c.Meeting).WithMany().HasForeignKey(static c => c.MeetingId).OnDelete(DeleteBehavior.Cascade);
            code.HasMany(static c => c.Uses).WithOne(static u => u.VotingCode).HasForeignKey(static u => u.VotingCodeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CodeUse>(static use =>
        {
            use.HasKey(static u => new { u.VotingCodeId, u.AgendaItemId });
            use.HasOne<AgendaItem>().WithMany().HasForeignKey(static u => u.AgendaItemId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClubSettings>(static club =>
        {
            club.Property(static c => c.Id).ValueGeneratedNever();
            club.Property(static c => c.ClubName).HasMaxLength(200);
            club.Property(static c => c.ContactEmail).HasMaxLength(320);
            club.Property(static c => c.Language).HasMaxLength(8);
            club.Property(static c => c.PrimaryColor).HasMaxLength(9);
            club.Property(static c => c.SecondaryColor).HasMaxLength(9);
            club.Property(static c => c.HeadingFont).HasMaxLength(60);
            club.Property(static c => c.BodyFont).HasMaxLength(60);
            club.Property(static c => c.MailFromName).HasMaxLength(200);
            club.Property(static c => c.MailFromAddress).HasMaxLength(320);
            club.Property(static c => c.MailReplyTo).HasMaxLength(320);
            club.Property(static c => c.AdvanceVotingArticle).HasMaxLength(100);
        });
        modelBuilder.Entity<Voter>(static voter =>
        {
            voter.Property(static v => v.Name).HasMaxLength(200);
            voter.Property(static v => v.Email).HasMaxLength(320);
            voter.Property(static v => v.GuardianName).HasMaxLength(200);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Voxa.WebApi.PostgresTests;

/// <summary>The switch from integer to GUID keys keeps existing meetings and every link between their rows.</summary>
[Collection(PostgresCollection.Name)]
public sealed class GuidIdMigrationTests(PostgresFixture database)
{
    private const string BeforeGuids = "20261126195238_AddLanguages";

    [Fact]
    public async Task ExistingDataKeepsItsLinksWhenTheKeysBecomeGuids()
    {
        var connection = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = $"voxa_guid_{Guid.NewGuid():N}" }.ConnectionString;
        await using var context = new VoxaContext(new DbContextOptionsBuilder<VoxaContext>().UseNpgsql(connection).Options);
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(BeforeGuids);
            _ = await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Meetings" ("Name", "Kind", "StartsAt", "AdvanceVoting", "Status", "CodesIssued", "CreatedAt")
                VALUES ('ALV', 0, now(), false, 1, 1, now()), ('Extra', 1, now(), false, 0, 0, now());
                INSERT INTO "AgendaItems" ("MeetingId", "Position", "Title", "Kind", "Majority", "Seats", "State", "AdvanceVoting")
                VALUES (1, 1, 'Begroting', 0, 0, 1, 2, false), (2, 1, 'Anders', 0, 0, 1, 0, false);
                UPDATE "Meetings" SET "ScreenItemId" = 1 WHERE "Id" = 1;
                INSERT INTO "ItemOptions" ("AgendaItemId", "Position", "Label", "Role") VALUES (1, 1, 'Voor', 0), (1, 2, 'Tegen', 1);
                INSERT INTO "Voters" ("MeetingId", "Name", "CodeSent") VALUES (1, 'Jan', true), (2, 'Kees', false);
                INSERT INTO "VotingCodes" ("Id", "MeetingId", "Hash") VALUES ('00000000-0000-0000-0000-000000000001', 1, 'hash');
                INSERT INTO "CodeUses" ("VotingCodeId", "AgendaItemId") VALUES ('00000000-0000-0000-0000-000000000001', 1);
                INSERT INTO "BallotTallies" ("AgendaItemId", "Channel", "Count", "Version") VALUES (1, 1, 5, gen_random_uuid());
                INSERT INTO "OptionTallies" ("ItemOptionId", "Channel", "Count", "Version") VALUES (1, 1, 3, gen_random_uuid()), (2, 1, 2, gen_random_uuid());
                """);

            await migrator.MigrateAsync();

            var meeting = await context.Meetings.Include(static m => m.Items).ThenInclude(static i => i.Options).ThenInclude(static o => o.Tallies)
                                       .Include(static m => m.Items).ThenInclude(static i => i.Ballots)
                                       .Include(static m => m.Voters)
                                       .SingleAsync(static m => m.Name == "ALV");
            var item = Assert.Single(meeting.Items);
            Assert.Equal("Begroting", item.Title);
            Assert.Equal(item.Id, meeting.ScreenItemId);
            Assert.Equal(5, Assert.Single(item.Ballots).Count);
            Assert.Equal(["Voor:3", "Tegen:2"], item.Options.OrderBy(static o => o.Position).Select(static o => $"{o.Label}:{o.Tallies.Single().Count}"));
            Assert.Equal("Jan", Assert.Single(meeting.Voters).Name);
            Assert.Equal(meeting.Id, (await context.VotingCodes.SingleAsync()).MeetingId);
            Assert.Equal(item.Id, (await context.CodeUses.SingleAsync()).AgendaItemId);
            var extra = await context.Meetings.Include(static m => m.Items).Include(static m => m.Voters).SingleAsync(static m => m.Name == "Extra");
            Assert.Equal("Anders", Assert.Single(extra.Items).Title);
            Assert.Equal("Kees", Assert.Single(extra.Voters).Name);

            // The recreated foreign keys still cascade.
            _ = context.Meetings.Remove(meeting);
            _ = await context.SaveChangesAsync();
            Assert.Equal(0, await context.CodeUses.CountAsync());
            Assert.Equal(0, await context.OptionTallies.CountAsync());
        }
        finally
        {
            _ = await context.Database.EnsureDeletedAsync();
        }
    }
}

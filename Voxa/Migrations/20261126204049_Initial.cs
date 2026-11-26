using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voxa.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ClubName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    HeadingFont = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    BodyFont = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    LogoDataUrl = table.Column<string>(type: "text", nullable: true),
                    MailFromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MailFromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    MailReplyTo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    AdvanceVotingMaxDays = table.Column<int>(type: "integer", nullable: false),
                    AdvanceVotingArticle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinorVoting = table.Column<int>(type: "integer", nullable: false),
                    AdultAge = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    AdvanceVoting = table.Column<bool>(type: "boolean", nullable: false),
                    AdvanceOpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CodesIssued = table.Column<int>(type: "integer", nullable: false),
                    CodesUsedAtArchive = table.Column<int>(type: "integer", nullable: true),
                    ScreenItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Majority = table.Column<int>(type: "integer", nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    AdvanceVoting = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Voters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    GuardianName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CodeSent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voters_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingCodes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BallotTallies",
                columns: table => new
                {
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotTallies", x => new { x.AgendaItemId, x.Channel });
                    table.ForeignKey(
                        name: "FK_BallotTallies_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemOptions_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeUses",
                columns: table => new
                {
                    VotingCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeUses", x => new { x.VotingCodeId, x.AgendaItemId });
                    table.ForeignKey(
                        name: "FK_CodeUses_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CodeUses_VotingCodes_VotingCodeId",
                        column: x => x.VotingCodeId,
                        principalTable: "VotingCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptionTallies",
                columns: table => new
                {
                    ItemOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionTallies", x => new { x.ItemOptionId, x.Channel });
                    table.ForeignKey(
                        name: "FK_OptionTallies_ItemOptions_ItemOptionId",
                        column: x => x.ItemOptionId,
                        principalTable: "ItemOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_MeetingId_Position",
                table: "AgendaItems",
                columns: new[] { "MeetingId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeUses_AgendaItemId",
                table: "CodeUses",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemOptions_AgendaItemId",
                table: "ItemOptions",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Voters_MeetingId",
                table: "Voters",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingCodes_Hash",
                table: "VotingCodes",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingCodes_MeetingId",
                table: "VotingCodes",
                column: "MeetingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BallotTallies");

            migrationBuilder.DropTable(
                name: "ClubSettings");

            migrationBuilder.DropTable(
                name: "CodeUses");

            migrationBuilder.DropTable(
                name: "OptionTallies");

            migrationBuilder.DropTable(
                name: "Voters");

            migrationBuilder.DropTable(
                name: "VotingCodes");

            migrationBuilder.DropTable(
                name: "ItemOptions");

            migrationBuilder.DropTable(
                name: "AgendaItems");

            migrationBuilder.DropTable(
                name: "Meetings");
        }
    }
}

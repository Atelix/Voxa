using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voxa.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Meetings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "ClubSettings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                // Existing clubs keep the Dutch they had.
                defaultValue: "nl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "ClubSettings");
        }
    }
}

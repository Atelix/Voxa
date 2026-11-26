using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voxa.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKeeva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeevaUrl",
                table: "ClubSettings");

            migrationBuilder.DropColumn(
                name: "MemberSource",
                table: "ClubSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeevaUrl",
                table: "ClubSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberSource",
                table: "ClubSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

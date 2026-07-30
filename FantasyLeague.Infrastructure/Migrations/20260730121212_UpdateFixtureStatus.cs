using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFixtureStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "league_fixtures",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "league_fixtures");
        }
    }
}

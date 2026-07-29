using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFixtureTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayScore",
                table: "league_fixtures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GameTime",
                table: "league_fixtures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeScore",
                table: "league_fixtures",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayScore",
                table: "league_fixtures");

            migrationBuilder.DropColumn(
                name: "GameTime",
                table: "league_fixtures");

            migrationBuilder.DropColumn(
                name: "HomeScore",
                table: "league_fixtures");
        }
    }
}

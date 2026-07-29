using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsePlayerStatsCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_player_stats",
                table: "player_stats");

            migrationBuilder.DropIndex(
                name: "IX_player_stats_NbaPlayerId_Season",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "player_stats");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_stats",
                table: "player_stats",
                columns: ["NbaPlayerId", "Season"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_player_stats",
                table: "player_stats");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "player_stats",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_stats",
                table: "player_stats",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_NbaPlayerId_Season",
                table: "player_stats",
                columns: ["NbaPlayerId", "Season"],
                unique: true);
        }
    }
}

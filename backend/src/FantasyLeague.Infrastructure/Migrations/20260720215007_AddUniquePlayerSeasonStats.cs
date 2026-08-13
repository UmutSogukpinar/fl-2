using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePlayerSeasonStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_stats_NbaPlayerId",
                table: "player_stats");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_NbaPlayerId_Season",
                table: "player_stats",
                columns: new[] { "NbaPlayerId", "Season" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_stats_NbaPlayerId_Season",
                table: "player_stats");

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_NbaPlayerId",
                table: "player_stats",
                column: "NbaPlayerId");
        }
    }
}

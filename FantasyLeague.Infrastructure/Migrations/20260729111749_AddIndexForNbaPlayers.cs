using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexForNbaPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_nba_players_FirstName",
                table: "nba_players",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_nba_players_LastName",
                table: "nba_players",
                column: "LastName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_nba_players_FirstName",
                table: "nba_players");

            migrationBuilder.DropIndex(
                name: "IX_nba_players_LastName",
                table: "nba_players");
        }
    }
}

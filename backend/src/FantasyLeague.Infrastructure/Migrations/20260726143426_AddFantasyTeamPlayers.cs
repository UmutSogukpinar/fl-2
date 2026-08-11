using System;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260726143426_AddFantasyTeamPlayers")]
public partial class AddFantasyTeamPlayers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "AK_fantasy_teams_Id_LeagueId",
            table: "fantasy_teams",
            columns: new[] { "Id", "LeagueId" });

        migrationBuilder.CreateTable(
            name: "fantasy_team_players",
            columns: table => new
            {
                FantasyTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                NbaPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                AcquiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fantasy_team_players", x => new { x.FantasyTeamId, x.NbaPlayerId });
                table.ForeignKey(
                    name: "FK_fantasy_team_players_fantasy_teams_FantasyTeamId_LeagueId",
                    columns: x => new { x.FantasyTeamId, x.LeagueId },
                    principalTable: "fantasy_teams",
                    principalColumns: new[] { "Id", "LeagueId" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_fantasy_team_players_nba_players_NbaPlayerId",
                    column: x => x.NbaPlayerId,
                    principalTable: "nba_players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_fantasy_team_players_FantasyTeamId_LeagueId",
            table: "fantasy_team_players",
            columns: new[] { "FantasyTeamId", "LeagueId" });
        migrationBuilder.CreateIndex(
            name: "IX_fantasy_team_players_LeagueId_NbaPlayerId",
            table: "fantasy_team_players",
            columns: new[] { "LeagueId", "NbaPlayerId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_fantasy_team_players_NbaPlayerId",
            table: "fantasy_team_players",
            column: "NbaPlayerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "fantasy_team_players");
        migrationBuilder.DropUniqueConstraint(
            name: "AK_fantasy_teams_Id_LeagueId",
            table: "fantasy_teams");
    }
}

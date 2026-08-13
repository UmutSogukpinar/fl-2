using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueFixturesAndDraftOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "draft_pick_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    PositionInRound = table.Column<int>(type: "integer", nullable: false),
                    OverallPick = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_pick_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_draft_pick_orders_fantasy_teams_TeamId_LeagueId",
                        columns: x => new { x.TeamId, x.LeagueId },
                        principalTable: "fantasy_teams",
                        principalColumns: new[] { "Id", "LeagueId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_draft_pick_orders_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "league_fixtures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Week = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwayTeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_fixtures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_league_fixtures_fantasy_teams_AwayTeamId_LeagueId",
                        columns: x => new { x.AwayTeamId, x.LeagueId },
                        principalTable: "fantasy_teams",
                        principalColumns: new[] { "Id", "LeagueId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_league_fixtures_fantasy_teams_HomeTeamId_LeagueId",
                        columns: x => new { x.HomeTeamId, x.LeagueId },
                        principalTable: "fantasy_teams",
                        principalColumns: new[] { "Id", "LeagueId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_league_fixtures_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_pick_orders_LeagueId_OverallPick",
                table: "draft_pick_orders",
                columns: new[] { "LeagueId", "OverallPick" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_pick_orders_LeagueId_Round_PositionInRound",
                table: "draft_pick_orders",
                columns: new[] { "LeagueId", "Round", "PositionInRound" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_pick_orders_TeamId_LeagueId",
                table: "draft_pick_orders",
                columns: new[] { "TeamId", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_league_fixtures_AwayTeamId_LeagueId",
                table: "league_fixtures",
                columns: new[] { "AwayTeamId", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_league_fixtures_HomeTeamId_LeagueId",
                table: "league_fixtures",
                columns: new[] { "HomeTeamId", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_league_fixtures_LeagueId_HomeTeamId_AwayTeamId",
                table: "league_fixtures",
                columns: new[] { "LeagueId", "HomeTeamId", "AwayTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_fixtures_LeagueId_Week",
                table: "league_fixtures",
                columns: new[] { "LeagueId", "Week" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_pick_orders");

            migrationBuilder.DropTable(
                name: "league_fixtures");
        }
    }
}

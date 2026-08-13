using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nba_players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NbaId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Team = table.Column<string>(type: "text", nullable: true),
                    Position = table.Column<string>(type: "text", nullable: true),
                    JerseyNumber = table.Column<int>(type: "integer", nullable: true),
                    HeightCm = table.Column<int>(type: "integer", nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nba_players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_stats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NbaPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    GamesStarted = table.Column<int>(type: "integer", nullable: false),
                    MinutesPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    PointsPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    ReboundsPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    AssistsPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    StealsPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    BlocksPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    TurnoversPerGame = table.Column<decimal>(type: "numeric", nullable: false),
                    FieldGoalPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    ThreePointPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    FreeThrowPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_stats_nba_players_NbaPlayerId",
                        column: x => x.NbaPlayerId,
                        principalTable: "nba_players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_stats_NbaPlayerId",
                table: "player_stats",
                column: "NbaPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_stats");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "nba_players");
        }
    }
}

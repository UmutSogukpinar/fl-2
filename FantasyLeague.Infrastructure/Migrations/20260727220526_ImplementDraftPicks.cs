using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementDraftPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NbaPlayerId",
                table: "draft_pick_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedAt",
                table: "draft_pick_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_pick_orders_LeagueId_NbaPlayerId",
                table: "draft_pick_orders",
                columns: new[] { "LeagueId", "NbaPlayerId" },
                unique: true,
                filter: "\"NbaPlayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_draft_pick_orders_NbaPlayerId",
                table: "draft_pick_orders",
                column: "NbaPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_draft_pick_orders_nba_players_NbaPlayerId",
                table: "draft_pick_orders",
                column: "NbaPlayerId",
                principalTable: "nba_players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_draft_pick_orders_nba_players_NbaPlayerId",
                table: "draft_pick_orders");

            migrationBuilder.DropIndex(
                name: "IX_draft_pick_orders_LeagueId_NbaPlayerId",
                table: "draft_pick_orders");

            migrationBuilder.DropIndex(
                name: "IX_draft_pick_orders_NbaPlayerId",
                table: "draft_pick_orders");

            migrationBuilder.DropColumn(
                name: "NbaPlayerId",
                table: "draft_pick_orders");

            migrationBuilder.DropColumn(
                name: "PickedAt",
                table: "draft_pick_orders");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transfer_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatingTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartyTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transfer_request_players",
                columns: table => new
                {
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    NbaPlayerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_request_players", x => new { x.TransferRequestId, x.FromTeamId, x.NbaPlayerId });
                    table.ForeignKey(
                        name: "FK_transfer_request_players_transfer_requests_TransferRequestId",
                        column: x => x.TransferRequestId,
                        principalTable: "transfer_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transfer_request_players");

            migrationBuilder.DropTable(
                name: "transfer_requests");
        }
    }
}

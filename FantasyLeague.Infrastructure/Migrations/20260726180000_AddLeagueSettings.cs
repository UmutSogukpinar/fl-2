using System;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260726180000_AddLeagueSettings")]
public partial class AddLeagueSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "league_settings",
            columns: table => new
            {
                LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                RosterSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 13),
                DraftDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DraftTimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_league_settings", x => x.LeagueId);
                table.ForeignKey(
                    name: "FK_league_settings_leagues_LeagueId",
                    column: x => x.LeagueId,
                    principalTable: "leagues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO "league_settings" ("LeagueId", "RosterSize", "DraftDate")
            SELECT "Id", 13, "DraftDate"
            FROM "leagues";
            """);

        migrationBuilder.DropColumn(
            name: "DraftDate",
            table: "leagues");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DraftDate",
            table: "leagues",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "leagues" AS l
            SET "DraftDate" = s."DraftDate"
            FROM "league_settings" AS s
            WHERE l."Id" = s."LeagueId";
            """);

        migrationBuilder.DropTable(name: "league_settings");
    }
}

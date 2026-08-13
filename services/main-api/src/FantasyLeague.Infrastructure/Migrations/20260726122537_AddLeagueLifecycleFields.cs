using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DraftDate",
                table: "leagues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "leagues",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "leagues",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Created");

            migrationBuilder.Sql(
                """
                UPDATE "leagues"
                SET "JoinCode" = UPPER(SUBSTRING(MD5("Id"::text) FROM 1 FOR 8))
                WHERE "JoinCode" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "JoinCode",
                table: "leagues",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_JoinCode",
                table: "leagues",
                column: "JoinCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leagues_JoinCode",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "DraftDate",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "leagues");

        }
    }
}

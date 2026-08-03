using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseLongLeagueFixtureId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_league_fixtures",
                table: "league_fixtures");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "league_fixtures");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "league_fixtures",
                type: "bigint",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_league_fixtures",
                table: "league_fixtures",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_league_fixtures",
                table: "league_fixtures");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "league_fixtures");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "league_fixtures",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_league_fixtures",
                table: "league_fixtures",
                column: "Id");
        }
    }
}

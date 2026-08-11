using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyLeague.Infrastructure.Migrations;

public partial class UpdateRefreshTokenUserIdColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE refresh_tokens
            ALTER COLUMN "UserId" TYPE uuid
            USING "UserId"::uuid;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE refresh_tokens
            ALTER COLUMN "UserId" TYPE text
            USING "UserId"::text;
            """);
    }
}
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FantasyLeague.Infrastructure.IntegrationTests.Database;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fantasy_league_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                transfer_request_players,
                transfer_requests,
                fantasy_team_players,
                draft_pick_orders,
                league_fixtures,
                fantasy_teams,
                player_stats,
                nba_players,
                league_settings,
                leagues,
                users
            RESTART IDENTITY CASCADE;
            """);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration";
}

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class NbaPlayerRepository(AppDbContext dbContext) : INbaPlayerRepository
{
    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<NbaPlayer>().AddRangeAsync(players, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<PlayerStats>()
            .Where(stats => stats.Season == season && nbaPlayerIds.Contains(stats.NbaPlayerId))
            .ToDictionaryAsync(stats => stats.NbaPlayerId, cancellationToken);
    }

    public Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<PlayerStats>().AddRangeAsync(playerStats, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<NbaPlayer?> GetByIdAsync(
        Guid id,
        PlayerResponseSize size,
        CancellationToken cancellationToken
    )
    {
        return size switch
        {
            PlayerResponseSize.Basic => GetByIdBasicAsync(
                                            id,
                                            cancellationToken),
            
            PlayerResponseSize.Detailed => GetByIdDetailedAsync(
                                                id,
                                                cancellationToken),

            PlayerResponseSize.Extended => GetByIdExtendedAsync(
                                                id,
                                                cancellationToken),

            _ => throw new ArgumentOutOfRangeException(
                    nameof(size), size, "Invalid player response size."),
        };
    }


    // ===================== Utils of GetByIdAsync() =====================

    private async Task<NbaPlayer?> GetByIdBasicAsync(
       Guid id,
       CancellationToken cancelllation)
    {
        var player = await dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new NbaPlayer
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Team = p.Team,
                Position = p.Position,
            })
            .SingleOrDefaultAsync(
                player => player.Id == id,
                cancelllation);

        return GetPlayerOrThrowAsync(ref id, ref player, cancelllation);
    }

    private async Task<NbaPlayer?> GetByIdDetailedAsync(
        Guid id,
        CancellationToken cancellation)
    {
        var player = await dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new NbaPlayer
            {
                Id = p.Id,
                NbaId = p.NbaId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Team = p.Team,
                Position = p.Position,
                JerseyNumber = p.JerseyNumber,
                HeightCm = p.HeightCm,
                WeightKg = p.WeightKg,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            })
            .SingleOrDefaultAsync(
                player => player.Id == id,
                cancellation);

        return GetPlayerOrThrowAsync(ref id, ref player, cancellation);
    }

    private async Task<NbaPlayer?> GetByIdExtendedAsync(
    Guid id,
    CancellationToken cancellation)
    {
        var player = await dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player => player.Id == id)
            .Select(player => new NbaPlayer
            {
                Id = player.Id,
                NbaId = player.NbaId,
                FirstName = player.FirstName,
                LastName = player.LastName,
                Team = player.Team,
                Position = player.Position,
                JerseyNumber = player.JerseyNumber,
                HeightCm = player.HeightCm,
                WeightKg = player.WeightKg,
                CreatedAt = player.CreatedAt,
                UpdatedAt = player.UpdatedAt,

                SeasonStats = player.SeasonStats
                    .Select(stats => new PlayerStats
                    {
                        Id = stats.Id,
                        NbaPlayerId = stats.NbaPlayerId,
                        Season = stats.Season,
                        GamesPlayed = stats.GamesPlayed,
                        PointsPerGame = stats.PointsPerGame,
                        AssistsPerGame = stats.AssistsPerGame,
                        ReboundsPerGame = stats.ReboundsPerGame,
                        StealsPerGame = stats.StealsPerGame,
                        BlocksPerGame = stats.BlocksPerGame
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellation);

        return GetPlayerOrThrowAsync(ref id, ref player, cancellation);
    }

    private NbaPlayer GetPlayerOrThrowAsync(
        ref Guid id,
        ref NbaPlayer? player,
        CancellationToken cancellationToken)
    {
        return player ??
            throw new NotFoundException(
                $"Player with Id '{id}' not found.");
    }
}

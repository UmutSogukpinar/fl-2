using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using Microsoft.EntityFrameworkCore;


namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{
    public async Task<IReadOnlyCollection<TeamRosterPlayerResponse>> GetRosterPlayersAsync(
        Guid teamId, CancellationToken cancellation)
    {
        return await (
            from roster in _dbContext.Set<FantasyTeamPlayer>().AsNoTracking()
            join player in _dbContext.Set<NbaPlayer>().AsNoTracking()
                on roster.NbaPlayerId equals player.Id
            where roster.FantasyTeamId == teamId
            orderby player.FirstName, player.LastName
            select new TeamRosterPlayerResponse(
                player.Id, player.FirstName, player.LastName,
                player.Team, player.Position))
            .ToArrayAsync(cancellation);
    }

    public async Task<IReadOnlyCollection<TransferResponse>> GetTransfersAsync(
        Guid teamId, CancellationToken cancellation)
    {
        var requests = await _dbContext.Set<TransferRequest>()
            .AsNoTracking()
            .Include(request => request.Players)
            .Where(request => request.InitiatingTeamId == teamId ||
                              request.CounterpartyTeamId == teamId)
            .OrderByDescending(request => request.CreatedAt)
            .ToArrayAsync(cancellation);
        var playerIds = requests.SelectMany(request => request.Players)
            .Select(player => player.NbaPlayerId).Distinct().ToArray();
        var names = await _dbContext.Set<NbaPlayer>().AsNoTracking()
            .Where(player => playerIds.Contains(player.Id))
            .ToDictionaryAsync(player => player.Id, cancellation);

        return requests.Select(request => new TransferResponse(
            request.Id,
            request.InitiatingTeamId,
            request.CounterpartyTeamId,
            request.Status,
            request.CreatedAt,
            request.ApprovedAt,
            request.Players.Select(item => new TransferPlayerResponse(
                item.NbaPlayerId,
                item.FromTeamId,
                names[item.NbaPlayerId].FirstName,
                names[item.NbaPlayerId].LastName)).ToArray())).ToArray();
    }

    public async Task<Guid> CreateTransferAsync(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IReadOnlyCollection<Guid> offeredPlayerIds,
        IReadOnlyCollection<Guid> requestedPlayerIds,
        CancellationToken cancellation
    )
    {
        await ValidateTransferAsync(initiatingTeamId, counterpartyTeamId, offeredPlayerIds, requestedPlayerIds, cancellation);

        var request = new TransferRequest
        {
            InitiatingTeamId = initiatingTeamId,
            CounterpartyTeamId = counterpartyTeamId,
            Players = offeredPlayerIds.Select(id => new TransferRequestPlayer
            {
                FromTeamId = initiatingTeamId,
                NbaPlayerId = id
            }).Concat(requestedPlayerIds.Select(id => new TransferRequestPlayer
            {
                FromTeamId = counterpartyTeamId,
                NbaPlayerId = id
            })).ToList()
        };
        _dbContext.Set<TransferRequest>().Add(request);
        await _dbContext.SaveChangesAsync(cancellation);
        return request.Id;
    }

    public async Task ApproveTransferAsync(
        Guid transferId, Guid approvingTeamId, CancellationToken cancellation)
    {
        var request = await _dbContext.Set<TransferRequest>()
            .Include(item => item.Players)
            .SingleOrDefaultAsync(item => item.Id == transferId, cancellation)
            ?? throw new NotFoundException($"Transfer request '{transferId}' was not found.");

        if (request.Status != TransferStatus.Pending)
            throw new ConflictException("Only pending transfer requests can be approved.");
        if (request.CounterpartyTeamId != approvingTeamId)
            throw new ConflictException("Only the receiving team can approve this transfer request.");

        var offeredIds = request.Players.Where(x => x.FromTeamId == request.InitiatingTeamId)
            .Select(x => x.NbaPlayerId).ToArray();
        var requestedIds = request.Players.Where(x => x.FromTeamId == request.CounterpartyTeamId)
            .Select(x => x.NbaPlayerId).ToArray();

        await ValidateTransferAsync(request.InitiatingTeamId, request.CounterpartyTeamId, offeredIds, requestedIds, cancellation);
        var leagueId = await _dbContext.Set<FantasyTeam>().Where(x => x.Id == request.InitiatingTeamId)
            .Select(x => x.LeagueId).SingleAsync(cancellation);
        var rosterPlayers = await _dbContext.Set<FantasyTeamPlayer>().Where(x =>
            (x.FantasyTeamId == request.InitiatingTeamId && offeredIds.Contains(x.NbaPlayerId)) ||
            (x.FantasyTeamId == request.CounterpartyTeamId && requestedIds.Contains(x.NbaPlayerId)))
            .ToArrayAsync(cancellation);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellation);
        _dbContext.Set<FantasyTeamPlayer>().RemoveRange(rosterPlayers);
        await _dbContext.SaveChangesAsync(cancellation);
        _dbContext.Set<FantasyTeamPlayer>().AddRange(
            offeredIds.Select(id => new FantasyTeamPlayer { FantasyTeamId = request.CounterpartyTeamId, LeagueId = leagueId, NbaPlayerId = id })
                .Concat(requestedIds.Select(id => new FantasyTeamPlayer { FantasyTeamId = request.InitiatingTeamId, LeagueId = leagueId, NbaPlayerId = id })));
        request.Status = TransferStatus.Approved;
        request.ApprovedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellation);
        await transaction.CommitAsync(cancellation);
    }

    private async Task ValidateTransferAsync(
        Guid initiatingTeamId, Guid counterpartyTeamId,
        IReadOnlyCollection<Guid> offeredPlayerIds, IReadOnlyCollection<Guid> requestedPlayerIds,
        CancellationToken cancellation)
    {
        var teams = await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.Id == initiatingTeamId || team.Id == counterpartyTeamId)
            .Select(team => new { team.Id, team.LeagueId })
            .ToArrayAsync(cancellation);

        if (teams.Length != 2)
            throw new NotFoundException("One or more fantasy teams were not found.");

        if (teams[0].LeagueId != teams[1].LeagueId)
        {
            throw new ConflictException(
                "Players can only be transferred between teams in the same league.");
        }

        var rosterPlayers = await _dbContext.Set<FantasyTeamPlayer>().AsNoTracking()
            .Where(player => player.FantasyTeamId == initiatingTeamId || player.FantasyTeamId == counterpartyTeamId)
            .ToArrayAsync(cancellation);

        if (offeredPlayerIds.Any(id => !rosterPlayers.Any(x => x.FantasyTeamId == initiatingTeamId && x.NbaPlayerId == id)) ||
            requestedPlayerIds.Any(id => !rosterPlayers.Any(x => x.FantasyTeamId == counterpartyTeamId && x.NbaPlayerId == id)))
            throw new NotFoundException("One or more players are not in the specified fantasy team.");

        var rosterSize = await _dbContext.Set<LeagueSettings>().Where(x => x.LeagueId == teams[0].LeagueId)
            .Select(x => x.RosterSize).SingleAsync(cancellation);
        var initiatingCount = rosterPlayers.Count(x => x.FantasyTeamId == initiatingTeamId) - offeredPlayerIds.Count + requestedPlayerIds.Count;
        var counterpartyCount = rosterPlayers.Count(x => x.FantasyTeamId == counterpartyTeamId) - requestedPlayerIds.Count + offeredPlayerIds.Count;
        if (initiatingCount < rosterSize || counterpartyCount < rosterSize)
            throw new ConflictException($"A transfer cannot reduce either roster below {rosterSize} players.");
    }

    public async Task<(int PlayerCount, int RosterSize)> GetRosterStateAsync(
        Guid teamId,
        CancellationToken cancellation
    )
    {
        var rosterSize = await (
                from team in _dbContext.Set<FantasyTeam>().AsNoTracking()
                join settings in _dbContext.Set<LeagueSettings>().AsNoTracking()
                    on team.LeagueId equals settings.LeagueId
                where team.Id == teamId
                select settings.RosterSize)
            .SingleAsync(cancellation);

        var playerCount = await _dbContext.Set<FantasyTeamPlayer>()
            .AsNoTracking()
            .CountAsync(player => player.FantasyTeamId == teamId, cancellation);

        return (playerCount, rosterSize);
    }

    public async Task ReleaseAPlayerAsync(
        Guid teamId,
        Guid playerId,
        CancellationToken cancellationToken
    )
    {
        var player = await _dbContext.Set<FantasyTeamPlayer>()
            .SingleOrDefaultAsync(
                player =>
                    player.FantasyTeamId == teamId &&
                    player.NbaPlayerId == playerId,
                cancellationToken);

        if (player is null)
        {
            throw new NotFoundException(
                $"NBA player '{playerId}'" +
                $" was not found in fantasy team '{teamId}'."
            );
        }

        _dbContext.Set<FantasyTeamPlayer>().Remove(player);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TradeValidationResult>
    ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
        Guid? homeId = null,
        Guid? awayId = null,
        Guid? playerId = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = TradeValidationResult.None;

        var requestedTeamIds = new[] { homeId, awayId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var existingTeamIds = await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => requestedTeamIds.Contains(team.Id))
            .Select(team => team.Id)
            .ToListAsync(cancellationToken);

        if (homeId.HasValue && !existingTeamIds.Contains(homeId.Value))
            result |= TradeValidationResult.HomeTeamNotFound;

        if (awayId.HasValue && !existingTeamIds.Contains(awayId.Value))
            result |= TradeValidationResult.AwayTeamNotFound;

        if (playerId.HasValue)
        {
            var playerExists = await _dbContext.Set<NbaPlayer>()
                .AsNoTracking()
                .AnyAsync(
                    player => player.Id == playerId.Value,
                    cancellationToken);

            if (!playerExists)
                result |= TradeValidationResult.PlayerNotFound;
        }

        return result;
    }
}

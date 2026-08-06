using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Transfers;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{
    public async Task<Guid> CreateTransferAsync(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IReadOnlyCollection<Guid> offeredPlayerIds,
        IReadOnlyCollection<Guid> requestedPlayerIds,
        CancellationToken cancellation)
    {
        await ValidateTransferAsync(
            initiatingTeamId, counterpartyTeamId,
            offeredPlayerIds, requestedPlayerIds, cancellation);

        var request = CreateTransferRequest(
            initiatingTeamId, counterpartyTeamId,
            offeredPlayerIds, requestedPlayerIds);
        _dbContext.Set<TransferRequest>().Add(request);
        await _dbContext.SaveChangesAsync(cancellation);
        return request.Id;
    }

    public async Task ApproveTransferAsync(
        Guid transferId, Guid approvingTeamId, CancellationToken cancellation)
    {
        var request = await GetPendingTransferAsync(transferId, cancellation);
        EnsureCanApprove(request, approvingTeamId);
        var selection = CreatePlayerSelection(request);

        await ValidateTransferAsync(
            request.InitiatingTeamId, request.CounterpartyTeamId,
            selection.OfferedIds, selection.RequestedIds, cancellation);
        await SwapRosterPlayersAsync(request, selection, cancellation);
    }

    private static TransferRequest CreateTransferRequest(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IEnumerable<Guid> offeredPlayerIds,
        IEnumerable<Guid> requestedPlayerIds)
    {
        return new TransferRequest
        {
            InitiatingTeamId = initiatingTeamId,
            CounterpartyTeamId = counterpartyTeamId,
            Players = CreateTransferPlayers(initiatingTeamId, offeredPlayerIds)
                .Concat(CreateTransferPlayers(counterpartyTeamId, requestedPlayerIds))
                .ToList()
        };
    }

    private static IEnumerable<TransferRequestPlayer> CreateTransferPlayers(
        Guid teamId, IEnumerable<Guid> playerIds)
    {
        return playerIds.Select(playerId => new TransferRequestPlayer
        {
            FromTeamId = teamId,
            NbaPlayerId = playerId
        });
    }

    private Task<TransferRequest?> FindTransferAsync(
        Guid transferId, CancellationToken cancellation)
    {
        return _dbContext.Set<TransferRequest>()
            .Include(request => request.Players)
            .SingleOrDefaultAsync(request => request.Id == transferId, cancellation);
    }

    private async Task<TransferRequest> GetPendingTransferAsync(
        Guid transferId, CancellationToken cancellation)
    {
        var request = await FindTransferAsync(transferId, cancellation);
        if (request is null)
            throw new NotFoundException($"Transfer request '{transferId}' was not found.");
        if (request.Status != TransferStatus.Pending)
            throw new ConflictException("Only pending transfer requests can be approved.");
        return request;
    }

    private static void EnsureCanApprove(
        TransferRequest request, Guid approvingTeamId)
    {
        if (request.CounterpartyTeamId != approvingTeamId)
            throw new ConflictException("Only the receiving team can approve this transfer request.");
    }

    private async Task SwapRosterPlayersAsync(
        TransferRequest request,
        TransferPlayerSelection selection,
        CancellationToken cancellation)
    {
        var leagueId = await GetLeagueIdAsync(request.InitiatingTeamId, cancellation);
        var currentPlayers = await GetTransferredRosterPlayersAsync(
            request, selection, cancellation);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellation);
        RemoveTransferredPlayers(currentPlayers);
        await _dbContext.SaveChangesAsync(cancellation);
        AddTransferredPlayers(request, selection, leagueId);
        MarkApproved(request);
        await _dbContext.SaveChangesAsync(cancellation);
        await transaction.CommitAsync(cancellation);
    }

    private Task<Guid> GetLeagueIdAsync(
        Guid teamId, CancellationToken cancellation)
    {
        return _dbContext.Set<FantasyTeam>()
            .Where(team => team.Id == teamId)
            .Select(team => team.LeagueId)
            .SingleAsync(cancellation);
    }

    private Task<FantasyTeamPlayer[]> GetTransferredRosterPlayersAsync(
        TransferRequest request,
        TransferPlayerSelection selection,
        CancellationToken cancellation)
    {
        return _dbContext.Set<FantasyTeamPlayer>()
            .Where(player =>
                (player.FantasyTeamId == request.InitiatingTeamId &&
                 selection.OfferedIds.Contains(player.NbaPlayerId)) ||
                (player.FantasyTeamId == request.CounterpartyTeamId &&
                 selection.RequestedIds.Contains(player.NbaPlayerId)))
            .ToArrayAsync(cancellation);
    }

    private void RemoveTransferredPlayers(IEnumerable<FantasyTeamPlayer> players)
    {
        _dbContext.Set<FantasyTeamPlayer>().RemoveRange(players);
    }

    private void AddTransferredPlayers(
        TransferRequest request,
        TransferPlayerSelection selection,
        Guid leagueId)
    {
        _dbContext.Set<FantasyTeamPlayer>().AddRange(
            CreateRosterPlayers(request.CounterpartyTeamId, leagueId, selection.OfferedIds)
                .Concat(CreateRosterPlayers(
                    request.InitiatingTeamId, leagueId, selection.RequestedIds)));
    }

    private static void MarkApproved(TransferRequest request)
    {
        request.Status = TransferStatus.Approved;
        request.ApprovedAt = DateTime.UtcNow;
    }

    private static TransferPlayerSelection CreatePlayerSelection(
        TransferRequest request)
    {
        return new TransferPlayerSelection(
            GetPlayerIds(request, request.InitiatingTeamId),
            GetPlayerIds(request, request.CounterpartyTeamId));
    }

    private static Guid[] GetPlayerIds(TransferRequest request, Guid teamId)
    {
        return request.Players
            .Where(player => player.FromTeamId == teamId)
            .Select(player => player.NbaPlayerId)
            .ToArray();
    }

    private static IEnumerable<FantasyTeamPlayer> CreateRosterPlayers(
        Guid teamId, Guid leagueId, IEnumerable<Guid> playerIds)
    {
        return playerIds.Select(playerId => new FantasyTeamPlayer
        {
            FantasyTeamId = teamId,
            LeagueId = leagueId,
            NbaPlayerId = playerId
        });
    }

    private sealed record TransferPlayerSelection(
        Guid[] OfferedIds, Guid[] RequestedIds);
}

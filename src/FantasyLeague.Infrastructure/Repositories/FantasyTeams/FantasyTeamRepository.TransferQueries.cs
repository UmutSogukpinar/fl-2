using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Entities.Transfers;

using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{
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

        var playerIds = requests
            .SelectMany(request => request.Players)
            .Select(player => player.NbaPlayerId)
            .Distinct()
            .ToArray();
        var playersById = await _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
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
                playersById[item.NbaPlayerId].FirstName,
                playersById[item.NbaPlayerId].LastName)).ToArray()))
            .ToArray();
    }
}

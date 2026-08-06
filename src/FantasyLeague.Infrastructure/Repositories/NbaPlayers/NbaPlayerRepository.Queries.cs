using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository
{
    public async Task<(IReadOnlyCollection<NbaPlayerBasicResponse> Items,
        int TotalCount)>
    GetPagedAsync(
        PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<NbaPlayer>().AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .ApplyPagination(request)
            .ToBasic()
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IPlayerResponse?> GetByIdAndSeasonAsync(
        Guid id,
        int season,
        PlayerResponseSize size,
        CancellationToken cancellationToken
    )
    {
        return size switch
        {
            PlayerResponseSize.Basic => await GetBasicAsync(
                                            id,
                                            cancellationToken
                                        ),

            PlayerResponseSize.Detailed => await GetDetailedAsync(
                                                id,
                                                cancellationToken),

            PlayerResponseSize.Extended => await GetExtendedAsync(
                                                id,
                                                season,
                                                cancellationToken
                                           ),

            _ => throw new ArgumentOutOfRangeException(
                    nameof(size), size, "Invalid player response size."),
        };
    }

    // ====================== Utils of GetByIdAndSeasonAsync() ======================

    private Task<NbaPlayerBasicResponse?> GetBasicAsync(
       Guid id,
       CancellationToken cancelllation
    )
    {
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToBasic()
            .SingleOrDefaultAsync(cancelllation);
    }

    private Task<NbaPlayerDetailedResponse?> GetDetailedAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToDetailed()
            .SingleOrDefaultAsync(cancellation);
    }

    private Task<NbaPlayerExtendedResponse?> GetExtendedAsync(
        Guid id,
        int season,
        CancellationToken cancellation
    )
    {
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player => player.Id == id)
            .ToExtended(season)
            .SingleOrDefaultAsync(cancellation);
    }
}

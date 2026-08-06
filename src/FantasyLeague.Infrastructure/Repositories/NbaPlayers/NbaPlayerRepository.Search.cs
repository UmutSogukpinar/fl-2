using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FantasyLeague.Application.Common.Pagination;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository
{
    public async Task<(
        IReadOnlyCollection<IPlayerResponse> Items,
        int TotalCount)> GetPagedNbaPlayersByNameAsync(
        PaginationRequest pagination,
        GetNbaPlayersRequest request,
        CancellationToken cancellationToken)
    {
        var query = CreateFilteredSearchQuery(request);
        var totalCount = await query.CountAsync(cancellationToken);
        var pagedQuery = ApplySearchPagination(query, pagination);
        var items = await ProjectSearchResultsAsync(
            pagedQuery,
            request,
            cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<NbaPlayer> CreateFilteredSearchQuery(
        GetNbaPlayersRequest request)
    {
        var query = _dbContext.Set<NbaPlayer>().AsNoTracking();

        if (request.Id != Guid.Empty)
        {
            query = query.Where(player => player.Id == request.Id);
        }

        if (request.Name != string.Empty)
        {
            query = query.Where(player => EF.Functions.ILike(
                player.FirstName,
                $"{request.Name}%"));
        }

        if (request.Surname != string.Empty)
        {
            query = query.Where(player => EF.Functions.ILike(
                player.LastName,
                $"{request.Surname}%"));
        }

        return query;
    }

    private static IQueryable<NbaPlayer> ApplySearchPagination(
        IQueryable<NbaPlayer> query,
        PaginationRequest pagination)
    {
        return query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .ThenBy(player => player.Id)
            .ApplyPagination(pagination);
    }

    private static async Task<IReadOnlyCollection<IPlayerResponse>>
        ProjectSearchResultsAsync(
            IQueryable<NbaPlayer> query,
            GetNbaPlayersRequest request,
            CancellationToken cancellationToken)
    {
        return request.Size switch
        {
            PlayerResponseSize.Basic => await query.ToBasic()
                .ToArrayAsync(cancellationToken),

            PlayerResponseSize.Detailed => await query.ToDetailed()
                .ToArrayAsync(cancellationToken),

            PlayerResponseSize.Extended => await query
                .ToExtended(request.Season)
                .ToArrayAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(
                nameof(request.Size),
                request.Size,
                "Invalid player response size.")
        };
    }
}

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Time;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.Common.Normalization;

namespace FantasyLeague.Application.Services.Leagues;

public sealed class LeagueService(
    ILeagueRepository leagueRepository,
    IFantasyTeamRepository teamRepository,
    ILeagueSetupRepository leagueSetupRepository,
    IUserRepository userRepository) : ILeagueService
{
    public async Task<PagedResponse<LeagueResponse>> GetAsync(
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        request.ValidatePaginationRequest();

        var (items, totalCount) = await 
            leagueRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken
            );

        return new PagedResponse<LeagueResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalCount.CalculateTotalPage(request.PageSize)
        );
    }

    public async Task<LeagueResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await leagueRepository.GetResponseByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    public async Task<LeagueResponse> CreateAsync(
        CreateLeagueRequest request,
        CancellationToken cancellation)
    {
        request = request.NormalizeCreateLeagueRequest();
        request.ValidateCreateLeagueRequest();

        var commissioner = await userRepository.GetResponseByIdAsync(
            request.CommissionerId,
            cancellation
            ) ?? throw new NotFoundException(
                $"User '{request.CommissionerId}' was not found.");

        var draftDateUtc = DateTimeUtcConverter.ConvertToUtc(
            request.DraftDate, commissioner.TimeZoneId);
        draftDateUtc!.Value.ValidateFutureDraftDate();
        var league = request.ToEntity(
            draftDateUtc, commissioner.TimeZoneId);
        var commissionerTeam = new FantasyTeam
        {
            Name = request.TeamName.GetCommissionerTeamName(
                commissioner.Username),
            LeagueId = league.Id,
            OwnerId = request.CommissionerId
        };

        await leagueRepository.AddAsync(league, cancellation);
        await teamRepository.AddAsync(commissionerTeam, cancellation);
        await leagueRepository.SaveChangesAsync(cancellation);

        return league.ToResponse();
    }

    public async Task<LeagueResponse> UpdateAsync(
        Guid id,
        UpdateLeagueRequest request,
        CancellationToken cancellation)
    {
        request = request.NormalizeUpdateLeagueRequest();
        request.ValidateUpdateLeagueRequest();

        var currentTeamCount = await teamRepository.CountByLeagueIdAsync(
            id,
            cancellation
        );

        if (request.MaxTeams < currentTeamCount)
        {
            throw new ConflictException(
                $"MaxTeams cannot be lower than" +
                $"the current team count ({currentTeamCount}).");
        }

        var league = await GetTrackedLeagueOrThrowAsync(
            id,
            cancellation
        );

        var commissioner = await userRepository.GetResponseByIdAsync(
            league.CommissionerId, cancellation)
            ?? throw new NotFoundException(
                $"User '{league.CommissionerId}' was not found.");

        var draftDateUtc = DateTimeUtcConverter.ConvertToUtc(
            request.DraftDate, commissioner.TimeZoneId);
        draftDateUtc!.Value.ValidateFutureDraftDate();
        request.MapTo(
            league, draftDateUtc, commissioner.TimeZoneId);

        await leagueRepository.SaveChangesAsync(cancellation);
        return league.ToResponse();
    }

    public async Task DeleteAsync(
        Guid id,
        Guid commissionerId,
        CancellationToken cancellation)
    {
        var league = await GetTrackedLeagueOrThrowAsync(
            id,
            cancellation
        );

        if (league.CommissionerId != commissionerId)
        {
            throw new ForbiddenException(
                "Only the league commissioner can cancel the league."
            );
        }

        if (league.Status is not (
            LeagueStatus.Created or
            LeagueStatus.RegistrationOpen or
            LeagueStatus.DraftDelayed))
        {
            throw new ConflictException(
                "Only a created, registration-open," +
                "or delayed league can be cancelled.");
        }

        leagueRepository.Remove(league);
        await leagueRepository.SaveChangesAsync(cancellation);
    }

    private async Task<League> GetTrackedLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await leagueRepository.GetTrackedByIdAsync(
            id,
            cancellation
        ) ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    public async Task<IReadOnlyList<LeagueFixtureResponse>>
    GetFixturesAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        _ = await leagueRepository.GetResponseByIdAsync(
            id,
            cancellation
        ) ?? throw new NotFoundException(
                $"League '{id}' was not found."
            );

        return await leagueSetupRepository.GetFixturesAsync(
            id,
            cancellation
        );
    }

    public async Task<IReadOnlyList<DraftPickOrderResponse>> 
    GetDraftOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        _ = await leagueRepository.GetResponseByIdAsync(
            id,
            cancellationToken
        ) ?? throw new NotFoundException($"League '{id}' was not found.");

        return await leagueSetupRepository.GetDraftOrderAsync(
            id,
            cancellationToken
        );
    }

}

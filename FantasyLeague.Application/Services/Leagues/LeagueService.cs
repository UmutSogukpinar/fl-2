using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.Leagues;

public sealed class LeagueService(
    ILeagueRepository leagueRepository,
    IFantasyTeamRepository teamRepository,
    IUserRepository userRepository) : ILeagueService
{
    public async Task<IReadOnlyCollection<LeagueResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await leagueRepository.GetAllAsync(cancellationToken);
    }

    public async Task<LeagueResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await leagueRepository.GetResponseByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    public async Task<LeagueResponse> CreateAsync(
        CreateLeagueRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);
        ValidateSeason(request.Season);
        ValidateMaxTeams(request.MaxTeams);

        _ = await userRepository.GetResponseByIdAsync(
            request.CommissionerId,
            cancellationToken)
            ?? throw new NotFoundException($"User '{request.CommissionerId}' was not found.");

        var league = request.ToEntity();

        await leagueRepository.AddAsync(league, cancellationToken);
        await leagueRepository.SaveChangesAsync(cancellationToken);
        return league.ToResponse();
    }

    public async Task<LeagueResponse> UpdateAsync(
        Guid id,
        UpdateLeagueRequest request,
        CancellationToken cancellationToken = default)
    {
        var league = await GetTrackedLeagueOrThrowAsync(id, cancellationToken);
        ValidateName(request.Name);
        ValidateMaxTeams(request.MaxTeams);

        var currentTeamCount = await teamRepository.CountByLeagueIdAsync(id, cancellationToken);

        if (request.MaxTeams < currentTeamCount)
        {
            throw new ConflictException(
                $"MaxTeams cannot be lower than the current team count ({currentTeamCount}).");
        }

        request.MapTo(league);

        await leagueRepository.SaveChangesAsync(cancellationToken);
        return league.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var league = await GetTrackedLeagueOrThrowAsync(id, cancellationToken);
        leagueRepository.Remove(league);
        await leagueRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<League> GetTrackedLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await leagueRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("League name is required.");
        }

        return name.Trim();
    }

    private static void ValidateSeason(int season)
    {
        if (season < 1946)
        {
            throw new BadRequestException("Season must be 1946 or later.");
        }
    }

    private static void ValidateMaxTeams(int maxTeams)
    {
        if (maxTeams < 2 || maxTeams > 30)
        {
            throw new BadRequestException("MaxTeams must be between 2 and 30.");
        }
    }

}

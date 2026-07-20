using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
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
        var leagues = await leagueRepository.GetAllAsync(cancellationToken);
        return leagues.Select(Map).ToArray();
    }

    public async Task<LeagueResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Map(await GetLeagueOrThrowAsync(id, cancellationToken));
    }

    public async Task<LeagueResponse> CreateAsync(
        CreateLeagueRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        ValidateSeason(request.Season);
        ValidateMaxTeams(request.MaxTeams);

        var commissioner = await userRepository.GetByIdAsync(
            request.CommissionerId,
            cancellationToken)
            ?? throw new NotFoundException($"User '{request.CommissionerId}' was not found.");

        var league = new League
        {
            Name = name,
            Description = NormalizeDescription(request.Description),
            Season = request.Season,
            MaxTeams = request.MaxTeams,
            CommissionerId = commissioner.Id,
            Commissioner = commissioner
        };

        await leagueRepository.AddAsync(league, cancellationToken);
        await leagueRepository.SaveChangesAsync(cancellationToken);
        return Map(league);
    }

    public async Task<LeagueResponse> UpdateAsync(
        Guid id,
        UpdateLeagueRequest request,
        CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueOrThrowAsync(id, cancellationToken);
        var name = ValidateName(request.Name);
        ValidateMaxTeams(request.MaxTeams);

        var currentTeamCount = await teamRepository.CountByLeagueIdAsync(id, cancellationToken);

        if (request.MaxTeams < currentTeamCount)
        {
            throw new ConflictException(
                $"MaxTeams cannot be lower than the current team count ({currentTeamCount}).");
        }

        league.Name = name;
        league.Description = NormalizeDescription(request.Description);
        league.MaxTeams = request.MaxTeams;
        league.UpdatedAt = DateTime.UtcNow;

        await leagueRepository.SaveChangesAsync(cancellationToken);
        return Map(league);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueOrThrowAsync(id, cancellationToken);
        leagueRepository.Remove(league);
        await leagueRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<League> GetLeagueOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await leagueRepository.GetByIdAsync(id, cancellationToken)
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

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static LeagueResponse Map(League league) => new(
        league.Id,
        league.Name,
        league.Description,
        league.Season,
        league.MaxTeams,
        league.CommissionerId,
        league.CreatedAt,
        league.UpdatedAt);
}

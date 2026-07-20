using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed class FantasyTeamService(
    IFantasyTeamRepository teamRepository,
    ILeagueRepository leagueRepository,
    IUserRepository userRepository) : IFantasyTeamService
{
    public async Task<IReadOnlyCollection<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellationToken);
        var teams = await teamRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
        return teams.Select(Map).ToArray();
    }

    public async Task<FantasyTeamResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Map(await GetTeamOrThrowAsync(id, cancellationToken));
    }

    public async Task<FantasyTeamResponse> CreateAsync(
        CreateFantasyTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        var league = await GetLeagueOrThrowAsync(request.LeagueId, cancellationToken);
        var owner = await userRepository.GetByIdAsync(request.OwnerId, cancellationToken)
            ?? throw new NotFoundException($"User '{request.OwnerId}' was not found.");

        var teamCount = await teamRepository.CountByLeagueIdAsync(request.LeagueId, cancellationToken);

        if (teamCount >= league.MaxTeams)
        {
            throw new ConflictException("The league has reached its team capacity.");
        }

        await EnsureUniqueAsync(
            request.LeagueId,
            request.OwnerId,
            name,
            null,
            cancellationToken);

        var team = new FantasyTeam
        {
            Name = name,
            LeagueId = league.Id,
            OwnerId = owner.Id,
            Owner = owner
        };

        await teamRepository.AddAsync(team, cancellationToken);
        await teamRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    public async Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await GetTeamOrThrowAsync(id, cancellationToken);
        var name = ValidateName(request.Name);

        await EnsureUniqueAsync(
            team.LeagueId,
            team.OwnerId,
            name,
            team.Id,
            cancellationToken);

        team.Name = name;
        team.UpdatedAt = DateTime.UtcNow;

        await teamRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await GetTeamOrThrowAsync(id, cancellationToken);
        teamRepository.Remove(team);
        await teamRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.ExistsAsync(
                leagueId,
                ownerId,
                name,
                excludedTeamId,
                cancellationToken))
        {
            throw new ConflictException(
                "The owner already has a team or the team name is already used in this league.");
        }
    }

    private async Task<League> GetLeagueOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await leagueRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    private async Task<FantasyTeam> GetTeamOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await teamRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Fantasy team '{id}' was not found.");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Team name is required.");
        }

        return name.Trim();
    }

    private static FantasyTeamResponse Map(FantasyTeam team) => new(
        team.Id,
        team.Name,
        team.LeagueId,
        team.OwnerId,
        team.CreatedAt,
        team.UpdatedAt);
}

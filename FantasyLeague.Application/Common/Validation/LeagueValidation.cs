using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Leagues;

namespace FantasyLeague.Application.Common.Validation;

internal static class LeagueValidation
{
    public static void ValidateCreateLeagueRequest(this CreateLeagueRequest request)
    {
        ValidateName(request.Name);
        ValidateSeason(request.Season);
        ValidateMaxTeams(request.MaxTeams);
        ValidateRosterSize(request.RosterSize);
        ValidateDraftDateProvided(request.DraftDate);
        ValidateCommissionerId(request.CommissionerId);
    }

    public static void ValidateUpdateLeagueRequest(this UpdateLeagueRequest request)
    {
        ValidateName(request.Name);
        ValidateMaxTeams(request.MaxTeams);
        ValidateRosterSize(request.RosterSize);
        ValidateDraftDateProvided(request.DraftDate);
    }

    public static void ValidateFutureDraftDate(this DateTime draftDateUtc)
    {
        if (draftDateUtc <= DateTime.UtcNow)
        {
            throw new BadRequestException("DraftDate must be in the future.");
        }
    }

    public static string GetCommissionerTeamName(this string? teamName, string username)
    {
        var resolvedName = string.IsNullOrWhiteSpace(teamName)
            ? $"{username}'s Team"
            : teamName.Trim();

        if (resolvedName.Length > 100)
        {
            throw new BadRequestException("Team name cannot exceed 100 characters.");
        }

        return resolvedName;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("League name is required.");
        }
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

    private static void ValidateRosterSize(int rosterSize)
    {
        if (rosterSize < 1 || rosterSize > 30)
        {
            throw new BadRequestException("RosterSize must be between 1 and 30.");
        }
    }

    private static void ValidateDraftDateProvided(DateTime draftDate)
    {
        if (draftDate == default)
        {
            throw new BadRequestException("DraftDate is required.");
        }
    }

    private static void ValidateCommissionerId(Guid commissionerId)
    {
        if (commissionerId == Guid.Empty)
        {
            throw new BadRequestException("CommissionerId is required.");
        }
    }
}

namespace FantasyLeague.Application.Models;

public sealed record MatchStats(
    TeamMatchStats HomeTeamStats,
    TeamMatchStats AwayTeamStats
);

public sealed record TeamMatchStats(
    Guid FantasyTeamId,
    int Season,
    int PlayerCount,
    int GamesPlayed,
    int GamesStarted,
    double MinutesPerGame,
    double PointsPerGame,
    double ReboundsPerGame,
    double AssistsPerGame,
    double StealsPerGame,
    double BlocksPerGame,
    double TurnoversPerGame,
    double FieldGoalPercentage,
    double ThreePointPercentage,
    double FreeThrowPercentage)
{
    public static TeamMatchStats Empty(Guid fantasyTeamId, int season) => new(
        fantasyTeamId,
        season,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0);
}

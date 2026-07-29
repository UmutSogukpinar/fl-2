using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Models;

public sealed record MatchStats(
    PlayerStats HomeTeamStats,
    PlayerStats AwayTeamStats
);
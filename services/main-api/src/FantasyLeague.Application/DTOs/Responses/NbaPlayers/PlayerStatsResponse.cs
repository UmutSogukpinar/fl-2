using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public sealed record PlayerStatsResponse(
    int Season,
    int GamesPlayed,
    double PointsPerGame,
    double ReboundsPerGame,
    double AssistsPerGame,
    double StealsPerGame,
    double BlocksPerGame,
    double TurnoversPerGame,
    double FieldGoalPercentage,
    double ThreePointPercentage,
    double FreeThrowPercentage,
    double MinutesPerGame
);

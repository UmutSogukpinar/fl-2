using FantasyLeague.Application.Models;

namespace FantasyLeague.Application.DTOs.Requests.NbaPlayers;

public sealed record GetNbaPlayersRequest(
        Guid Id = default,
        string Name = "",
        string Surname = "",
        int Season = 2024,
        PlayerResponseSize Size = PlayerResponseSize.Basic
    );


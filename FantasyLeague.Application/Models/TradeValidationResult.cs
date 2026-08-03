namespace FantasyLeague.Application.Models;

[Flags]
public enum TradeValidationResult
{
    None = 0,
    HomeTeamNotFound = 1 << 0,
    AwayTeamNotFound = 1 << 1,
    PlayerNotFound = 1 << 2,
}

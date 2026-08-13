namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

public sealed class ApiSportsOptions
{
    public const string SectionName = "ApiSports";

    public string BaseUrl { get; init; } = "https://v2.nba.api-sports.io";

    public string ApiKey { get; init; } = string.Empty;
}

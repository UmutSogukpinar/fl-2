using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

internal sealed record ApiResponse<T>(
    [property: JsonPropertyName("response")]
    IReadOnlyCollection<T>? Response,
    [property: JsonPropertyName("errors")]
    JsonElement Errors);

internal sealed record ApiPlayer(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("firstname")]
    string FirstName,
    [property: JsonPropertyName("lastname")]
    string LastName,
    [property: JsonPropertyName("height")]
    ApiHeight? Height,
    [property: JsonPropertyName("weight")]
    ApiWeight? Weight,
    [property: JsonPropertyName("leagues")]
    ApiLeagues? Leagues);

internal sealed record ApiTeam(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("code")]
    string? Code,
    [property: JsonPropertyName("allStar")]
    bool AllStar,
    [property: JsonPropertyName("nbaFranchise")]
    bool NbaFranchise);

internal sealed record ApiHeight(
    [property: JsonPropertyName("meters")]
    string? Meters);
internal sealed record ApiWeight(
    [property: JsonPropertyName("kilograms")]
    string? Kilograms);
internal sealed record ApiLeagues(
    [property: JsonPropertyName("standard")]
    ApiStandardLeague? Standard);
internal sealed record ApiStandardLeague(
    [property: JsonPropertyName("jersey")]
    int? JerseyNumber,
    [property: JsonPropertyName("active")]
    bool Active,
    [property: JsonPropertyName("pos")]
    string? Position);

internal sealed record ApiPlayerStats(
    [property: JsonPropertyName("player")]
    ApiStatsPlayer Player,
    [property: JsonPropertyName("team")]
    ApiStatsTeam? Team,
    [property: JsonPropertyName("game")]
    ApiStatsGame Game,
    [property: JsonPropertyName("points")]
    int? Points,
    [property: JsonPropertyName("pos")]
    string? Position,
    [property: JsonPropertyName("min")]
    string? Minutes,
    [property: JsonPropertyName("fgm")]
    int? FieldGoalsMade,
    [property: JsonPropertyName("fga")]
    int? FieldGoalsAttempted,
    [property: JsonPropertyName("ftm")]
    int? FreeThrowsMade,
    [property: JsonPropertyName("fta")]
    int? FreeThrowsAttempted,
    [property: JsonPropertyName("tpm")]
    int? ThreePointersMade,
    [property: JsonPropertyName("tpa")]
    int? ThreePointersAttempted,
    [property: JsonPropertyName("totReb")]
    int? TotalRebounds,
    [property: JsonPropertyName("assists")]
    int? Assists,
    [property: JsonPropertyName("steals")]
    int? Steals,
    [property: JsonPropertyName("turnovers")]
    int? Turnovers,
    [property: JsonPropertyName("blocks")]
    int? Blocks);

internal sealed record ApiStatsPlayer(
    [property: JsonPropertyName("id")]
    int Id);
internal sealed record ApiStatsTeam(
    [property: JsonPropertyName("code")]
    string? Code);
internal sealed record ApiStatsGame(
    [property: JsonPropertyName("id")]
    int Id);

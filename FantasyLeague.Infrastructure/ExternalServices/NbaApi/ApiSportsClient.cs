using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

public sealed class ApiSportsClient(
    HttpClient httpClient,
    IOptions<ApiSportsOptions> options) : INbaPlayersApiClient
{
    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(6.2);
    private static readonly TimeSpan RateLimitRetryDelay = TimeSpan.FromMinutes(1);
    private readonly ApiSportsOptions _options = options.Value;
    private IReadOnlyCollection<ApiTeam>? _teams;
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public async Task<IReadOnlyCollection<ExternalNbaPlayer>> GetActivePlayersAsync(
        int season,
        CancellationToken cancellationToken)
    {
        var teams = await GetTeamsAsync(cancellationToken);
        var players = new Dictionary<int, ExternalNbaPlayer>();

        foreach (var team in teams)
        {
            var payload = await GetAsync<ApiPlayer>(
                $"/players?team={team.Id}&season={season}",
                cancellationToken);

            foreach (var player in payload.Response.Where(IsActivePlayer))
            {
                players[player.Id] = MapPlayer(player, team.Code);
            }
        }

        return players.Values.ToArray();
    }

    public async Task<IReadOnlyCollection<ExternalPlayerGameStats>> GetPlayerStatisticsAsync(
        int season,
        CancellationToken cancellationToken)
    {
        var teams = await GetTeamsAsync(cancellationToken);
        var statistics = new List<ExternalPlayerGameStats>();

        foreach (var team in teams)
        {
            var payload = await GetAsync<ApiPlayerStats>(
                $"/players/statistics?team={team.Id}&season={season}",
                cancellationToken);
            statistics.AddRange(payload.Response.Select(MapStats));
        }

        return statistics;
    }

    private async Task<IReadOnlyCollection<ApiTeam>> GetTeamsAsync(
        CancellationToken cancellationToken)
    {
        if (_teams is not null)
        {
            return _teams;
        }

        var payload = await GetAsync<ApiTeam>("/teams", cancellationToken);
        _teams = payload.Response
            .Where(team => team.NbaFranchise && !team.AllStar)
            .ToArray();

        return _teams;
    }

    private async Task<ApiResponse<T>> GetAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        EnsureApiKeyExists();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await WaitForRequestSlotAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.TryAddWithoutValidation("x-apisports-key", _options.ApiKey);

            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ExternalServiceException(
                        $"API-SPORTS returned status code {(int)response.StatusCode}.");
                }

                var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
                    cancellationToken: cancellationToken);

                if (payload is null)
                {
                    throw new ExternalServiceException("API-SPORTS returned an empty response.");
                }

                if (!HasErrors(payload.Errors))
                {
                    return payload;
                }

                var errorMessage = GetErrorMessage(payload.Errors);

                if (attempt < 3 && IsRateLimitError(errorMessage))
                {
                    await Task.Delay(RateLimitRetryDelay, cancellationToken);
                    continue;
                }

                throw new ExternalServiceException(
                    $"API-SPORTS rejected the request: {errorMessage}");
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                throw new ExternalServiceException("API-SPORTS could not be reached.", exception);
            }
            catch (JsonException exception)
            {
                throw new ExternalServiceException("API-SPORTS returned an invalid response.", exception);
            }
        }

        throw new ExternalServiceException("API-SPORTS request could not be completed.");
    }

    private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
    {
        var delay = _nextRequestAt - DateTimeOffset.UtcNow;

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        _nextRequestAt = DateTimeOffset.UtcNow.Add(RequestInterval);
    }

    private void EnsureApiKeyExists()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ExternalServiceException("API-SPORTS API key is not configured.");
        }
    }

    private static bool IsActivePlayer(ApiPlayer player)
    {
        return player.Leagues?.Standard?.Active == true;
    }

    private static ExternalNbaPlayer MapPlayer(ApiPlayer player, string? team) => new(
        player.Id,
        player.FirstName,
        player.LastName,
        team,
        player.Leagues?.Standard?.Position,
        player.Leagues?.Standard?.JerseyNumber,
        ConvertMetersToCentimeters(player.Height?.Meters),
        ParseDecimal(player.Weight?.Kilograms));

    private static ExternalPlayerGameStats MapStats(ApiPlayerStats stats) => new(
        stats.Player.Id,
        stats.Game.Id,
        stats.Team?.Code,
        stats.Position,
        ParseMinutes(stats.Minutes),
        stats.Points ?? 0,
        stats.TotalRebounds ?? 0,
        stats.Assists ?? 0,
        stats.Steals ?? 0,
        stats.Blocks ?? 0,
        stats.Turnovers ?? 0,
        stats.FieldGoalsMade ?? 0,
        stats.FieldGoalsAttempted ?? 0,
        stats.ThreePointersMade ?? 0,
        stats.ThreePointersAttempted ?? 0,
        stats.FreeThrowsMade ?? 0,
        stats.FreeThrowsAttempted ?? 0);

    private static int? ConvertMetersToCentimeters(string? meters)
    {
        var value = ParseDecimal(meters);
        return value.HasValue ? (int)Math.Round(value.Value * 100) : null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
    }

    private static decimal ParseMinutes(string? minutes)
    {
        if (string.IsNullOrWhiteSpace(minutes))
        {
            return 0;
        }

        var parts = minutes.Split(':');

        if (!decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }

        if (parts.Length == 2
            && decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds))
        {
            value += seconds / 60;
        }

        return value;
    }

    private static bool HasErrors(JsonElement errors)
    {
        return errors.ValueKind switch
        {
            JsonValueKind.Object => errors.EnumerateObject().Any(),
            JsonValueKind.Array => errors.GetArrayLength() > 0,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(errors.GetString()),
            _ => false
        };
    }

    private static string GetErrorMessage(JsonElement errors)
    {
        if (errors.ValueKind == JsonValueKind.Object)
        {
            return string.Join(
                " ",
                errors.EnumerateObject().Select(error => error.Value.ToString()));
        }

        return errors.ToString();
    }

    private static bool IsRateLimitError(string message)
    {
        return message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("too many requests", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("response")] IReadOnlyCollection<T> Response,
        [property: JsonPropertyName("errors")] JsonElement Errors);

    private sealed record ApiPlayer(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("firstname")] string FirstName,
        [property: JsonPropertyName("lastname")] string LastName,
        [property: JsonPropertyName("height")] ApiHeight? Height,
        [property: JsonPropertyName("weight")] ApiWeight? Weight,
        [property: JsonPropertyName("leagues")] ApiLeagues? Leagues);

    private sealed record ApiTeam(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("allStar")] bool AllStar,
        [property: JsonPropertyName("nbaFranchise")] bool NbaFranchise);

    private sealed record ApiHeight(
        [property: JsonPropertyName("meters")] string? Meters);

    private sealed record ApiWeight(
        [property: JsonPropertyName("kilograms")] string? Kilograms);

    private sealed record ApiLeagues(
        [property: JsonPropertyName("standard")] ApiStandardLeague? Standard);

    private sealed record ApiStandardLeague(
        [property: JsonPropertyName("jersey")] int? JerseyNumber,
        [property: JsonPropertyName("active")] bool Active,
        [property: JsonPropertyName("pos")] string? Position);

    private sealed record ApiPlayerStats(
        [property: JsonPropertyName("player")] ApiStatsPlayer Player,
        [property: JsonPropertyName("team")] ApiStatsTeam? Team,
        [property: JsonPropertyName("game")] ApiStatsGame Game,
        [property: JsonPropertyName("points")] int? Points,
        [property: JsonPropertyName("pos")] string? Position,
        [property: JsonPropertyName("min")] string? Minutes,
        [property: JsonPropertyName("fgm")] int? FieldGoalsMade,
        [property: JsonPropertyName("fga")] int? FieldGoalsAttempted,
        [property: JsonPropertyName("ftm")] int? FreeThrowsMade,
        [property: JsonPropertyName("fta")] int? FreeThrowsAttempted,
        [property: JsonPropertyName("tpm")] int? ThreePointersMade,
        [property: JsonPropertyName("tpa")] int? ThreePointersAttempted,
        [property: JsonPropertyName("totReb")] int? TotalRebounds,
        [property: JsonPropertyName("assists")] int? Assists,
        [property: JsonPropertyName("steals")] int? Steals,
        [property: JsonPropertyName("turnovers")] int? Turnovers,
        [property: JsonPropertyName("blocks")] int? Blocks);

    private sealed record ApiStatsPlayer(
        [property: JsonPropertyName("id")] int Id);

    private sealed record ApiStatsTeam(
        [property: JsonPropertyName("code")] string? Code);

    private sealed record ApiStatsGame(
        [property: JsonPropertyName("id")] int Id);
}

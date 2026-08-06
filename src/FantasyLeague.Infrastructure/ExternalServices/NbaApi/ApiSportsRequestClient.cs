using System.Net.Http.Json;
using System.Text.Json;
using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

internal sealed class ApiSportsRequestClient
{
    private const int MaximumAttempts = 3;
    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(6.2);
    private static readonly TimeSpan RateLimitRetryDelay = TimeSpan.FromMinutes(1);

    private readonly HttpClient _httpClient;
    private readonly ApiSportsOptions _options;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public ApiSportsRequestClient(HttpClient httpClient, ApiSportsOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyCollection<T>> GetResponseAsync<T>(
        string path,
        CancellationToken cancellation)
    {
        EnsureApiKeyExists();

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            await WaitForRequestSlotAsync(cancellation);
            var payload = await SendAsync<T>(path, cancellation);
            if (!ApiSportsErrorParser.HasErrors(payload.Errors))
            {
                return payload.Response
                    ?? throw new ExternalServiceException("API-SPORTS returned an empty response.");
            }

            var message = ApiSportsErrorParser.GetMessage(payload.Errors);
            if (attempt < MaximumAttempts && ApiSportsErrorParser.IsRateLimitError(message))
            {
                await Task.Delay(RateLimitRetryDelay, cancellation);
                continue;
            }

            throw new ExternalServiceException($"API-SPORTS rejected the request: {message}");
        }

        throw new ExternalServiceException("API-SPORTS request could not be completed.");
    }

    private async Task<ApiResponse<T>> SendAsync<T>(
        string path,
        CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("x-apisports-key", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    $"API-SPORTS returned status code {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
                       cancellationToken: cancellation)
                   ?? throw new ExternalServiceException("API-SPORTS returned an empty response.");
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

    private async Task WaitForRequestSlotAsync(CancellationToken cancellation)
    {
        await _requestGate.WaitAsync(cancellation);
        try
        {
            var delay = _nextRequestAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellation);
            _nextRequestAt = DateTimeOffset.UtcNow.Add(RequestInterval);
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private void EnsureApiKeyExists()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ExternalServiceException("API-SPORTS API key is not configured.");
    }
}

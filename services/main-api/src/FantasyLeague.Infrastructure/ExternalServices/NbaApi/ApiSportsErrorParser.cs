using System.Text.Json;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

internal static class ApiSportsErrorParser
{
    public static bool HasErrors(JsonElement errors) => errors.ValueKind switch
    {
        JsonValueKind.Object => errors.EnumerateObject().Any(),
        JsonValueKind.Array => errors.GetArrayLength() > 0,
        JsonValueKind.String => !string.IsNullOrWhiteSpace(errors.GetString()),
        _ => false
    };

    public static string GetMessage(JsonElement errors) =>
        errors.ValueKind == JsonValueKind.Object
            ? string.Join(" ", errors.EnumerateObject().Select(error => error.Value.ToString()))
            : errors.ToString();

    public static bool IsRateLimitError(string message) =>
        message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
        || message.Contains("too many requests", StringComparison.OrdinalIgnoreCase);
}

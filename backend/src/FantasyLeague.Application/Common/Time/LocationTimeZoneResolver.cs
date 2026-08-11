using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.Application.Common.Time;

public static class LocationTimeZoneResolver
{
    private static readonly IReadOnlyDictionary<string, string> TimeZones =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UTC"] = "UTC",
            ["Istanbul"] = "Europe/Istanbul",
            ["London"] = "Europe/London",
            ["New York"] = "America/New_York",
            ["Berlin"] = "Europe/Berlin"
        };

    public static string Resolve(string location)
    {
        if (string.IsNullOrWhiteSpace(location)
            || !TimeZones.TryGetValue(location.Trim(), out var timeZoneId))
        {
            throw new BadRequestException("The supplied location is not supported.");
        }

        return timeZoneId;
    }
}

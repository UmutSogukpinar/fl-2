namespace FantasyLeague.WebApi.Configuration;

public static class DotEnvConfigurationExtensions
{
    public static IConfigurationBuilder AddDotEnvFile(
        this IConfigurationBuilder configuration,
        string path)
    {
        if (!File.Exists(path))
        {
            return configuration;
        }

        var values = File.ReadLines(path)
            .Select(ParseLine)
            .Where(entry => entry.HasValue)
            .Select(entry => entry.GetValueOrDefault())
            .ToDictionary(
                entry => entry.Key.Replace("__", ":"),
                entry => (string?)entry.Value,
                StringComparer.OrdinalIgnoreCase);

        return configuration.AddInMemoryCollection(values);
    }

    private static KeyValuePair<string, string>? ParseLine(string line)
    {
        var trimmedLine = line.Trim().TrimStart('\uFEFF');

        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
        {
            return null;
        }

        if (trimmedLine.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
        {
            trimmedLine = trimmedLine[7..].TrimStart();
        }

        var separatorIndex = trimmedLine.IndexOf('=');

        if (separatorIndex <= 0)
        {
            return null;
        }

        var key = trimmedLine[..separatorIndex].Trim();
        var value = RemoveMatchingQuotes(trimmedLine[(separatorIndex + 1)..].Trim());

        return new KeyValuePair<string, string>(key, value);
    }

    private static string RemoveMatchingQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}

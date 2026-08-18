using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.WebApi.Options;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    [Required]
    [Url]
    public string OtlpEndpoint { get; init; } = string.Empty;
}

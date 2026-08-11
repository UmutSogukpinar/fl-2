using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.WebApi.Options;

public sealed class LokiOptions
{
    public const string SectionName = "Loki";

    [Required]
    [Url]
    public string Url { get; init; } = string.Empty;
}

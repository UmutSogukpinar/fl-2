using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.WebApi.Options;

public sealed class AlloyOptions
{
    public const string SectionName = "Alloy";

    [Required]
    [Url]
    public string OtlpEndpoint { get; init; } = string.Empty;
}

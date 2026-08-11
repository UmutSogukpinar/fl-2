using Microsoft.Extensions.Options;

namespace FantasyLeague.WebApi.Extensions;

public static class ConfigurationExtensions
{
    public static TOptions GetRequiredOptions<TOptions>(
        this IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        return configuration
            .GetRequiredSection(sectionName)
            .Get<TOptions>()
            ?? throw new InvalidOperationException(
                $"'{sectionName}' configuration section could not be bound to {typeof(TOptions).Name}.");
    }

    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = configuration.GetRequiredSection(sectionName);

        return services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}

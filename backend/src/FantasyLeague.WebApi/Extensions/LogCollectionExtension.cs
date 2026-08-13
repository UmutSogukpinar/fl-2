using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using FantasyLeague.WebApi.Options;

namespace FantasyLeague.WebApi.Extensions;

public static class LogCollectionExtension
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<AlloyOptions>(
            builder.Configuration,
            AlloyOptions.SectionName);

        var alloyOptions = builder.Configuration.GetRequiredOptions<AlloyOptions>(
            AlloyOptions.SectionName);
        var alloyEndpoint = new Uri(alloyOptions.OtlpEndpoint);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .UseGrafana()
                .AddOtlpExporter(options => ConfigureExporter(options, alloyEndpoint)));

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.UseGrafana();
            options.AddOtlpExporter(exporter => ConfigureExporter(exporter, alloyEndpoint));
        });

        return builder;
    }

    private static void ConfigureExporter(OtlpExporterOptions options, Uri endpoint)
    {
        options.Endpoint = endpoint;
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
}

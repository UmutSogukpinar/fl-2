using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using FantasyLeague.WebApi.Options;
using OpenTelemetry.Metrics;

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
                .AddOtlpExporter(options => ConfigureExporter(options, alloyEndpoint, "v1/traces")))
            .WithMetrics(metrics => metrics
                .UseGrafana()
                .AddOtlpExporter(options => ConfigureExporter(options, alloyEndpoint, "v1/metrics")));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.UseGrafana();
            logging.AddOtlpExporter(exporter => ConfigureExporter(exporter, alloyEndpoint, "v1/logs"));
        });

        return builder;
    }

    private static void ConfigureExporter(OtlpExporterOptions options, Uri endpoint, string signalPath)
    {
        options.Endpoint = new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}/{signalPath}");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
}

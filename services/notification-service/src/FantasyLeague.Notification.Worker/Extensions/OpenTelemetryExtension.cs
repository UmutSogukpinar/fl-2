using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using FantasyLeague.Notification.Worker.Configuration.OpenTelemetry;

namespace FantasyLeague.Notification.Worker.Extensions;

internal static class OpenTelemetryExtension
{
    public static HostApplicationBuilder ConfigureLogging(
        this HostApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<OpenTelemetryOptions>(
            builder.Configuration,
            OpenTelemetryOptions.SectionName);

        var telemetryOptions = builder.Configuration
            .GetRequiredOptions<OpenTelemetryOptions>(
                OpenTelemetryOptions.SectionName
            );

        var alloyEndpoint = new Uri(telemetryOptions.OtlpEndpoint);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .UseGrafana()
                .AddOtlpExporter(options => ConfigureExporter(options, alloyEndpoint, "v1/traces")));

        builder.Services.AddOpenTelemetry()
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
        options.Endpoint = new Uri(
            $"{endpoint.AbsoluteUri.TrimEnd('/')}/{signalPath}");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
}
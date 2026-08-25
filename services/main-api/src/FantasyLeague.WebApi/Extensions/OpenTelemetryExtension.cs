using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using FantasyLeague.WebApi.Options;
using OpenTelemetry.Metrics;

namespace FantasyLeague.WebApi.Extensions;

public static class OpenTelemetryExtension
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<OpenTelemetryOptions>(
            builder.Configuration,
            OpenTelemetryOptions.SectionName);

        var telemetryOptions = builder.Configuration
            .GetRequiredOptions<OpenTelemetryOptions>(
                OpenTelemetryOptions.SectionName
            );

        var otlpEndpoint = new Uri(telemetryOptions.OtlpEndpoint);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .UseGrafana()
                .AddOtlpExporter(options => ConfigureExporter(options, otlpEndpoint, "v1/traces")));

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .UseGrafana()
                .AddOtlpExporter(options => ConfigureExporter(options, otlpEndpoint, "v1/metrics")));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.UseGrafana();
            logging.AddOtlpExporter(exporter => ConfigureExporter(exporter, otlpEndpoint, "v1/logs"));
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

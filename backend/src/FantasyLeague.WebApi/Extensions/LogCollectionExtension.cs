using Grafana.OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using FantasyLeague.WebApi.Options;

namespace FantasyLeague.WebApi.Extensions;

public static class LogCollectionExtension
{
    // TODO: Remove ConsoleExporter for metrics and traces

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<LokiOptions>(
            builder.Configuration,
            LokiOptions.SectionName);

        var lokiOptions = builder.Configuration.GetRequiredOptions<LokiOptions>(
            LokiOptions.SectionName);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // builder.Services.AddOpenTelemetry()
            // .WithTracing(configure =>
            // {
            //    configure.UseGrafana()
             //       .AddConsoleExporter();
            // });
            //.WithMetrics(configure =>
            // {
            //    configure.UseGrafana()
            //        .AddConsoleExporter();
            // });

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.UseGrafana();
            options.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(lokiOptions.Url);
                exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        });

        return builder;
    }
}

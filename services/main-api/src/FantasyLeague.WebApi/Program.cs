using FantasyLeague.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOpenTelemetry();
builder.Services
    .AddWebApiServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddPersistenceAndBackgroundJobs(builder.Configuration);

var app = builder.Build();

await app.InitializeDevelopmentDatabaseAsync();

app.ConfigureRequestPipeline();
app.ConfigureRecurringJobs();
app.MapApiEndpoints();

app.Run();

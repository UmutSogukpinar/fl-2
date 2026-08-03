using Microsoft.EntityFrameworkCore;

using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.ExternalServices.NbaApi;
using FantasyLeague.Infrastructure.Repositories;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Security;
using FantasyLeague.Application.Common.Interfaces.ExternalServices;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.WebApi.ExceptionHandlers;
using FantasyLeague.WebApi.Hubs;
using FantasyLeague.WebApi.BackgroundServices;
using FantasyLeague.Infrastructure.Caching;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<INbaPlayerSyncService, NbaPlayerSyncService>();
builder.Services.AddScoped<INbaPlayerService, NbaPlayerService>();
builder.Services.AddScoped<INbaPlayerRepository, NbaPlayerRepository>();
builder.Services.AddScoped<ILeagueService, LeagueService>();
builder.Services.AddScoped<ILeagueRepository, LeagueRepository>();
builder.Services.AddScoped<ILeagueSetupRepository, LeagueSetupRepository>();
builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IDraftService, DraftService>();
builder.Services.AddHostedService<DraftSchedulerService>();
builder.Services.AddHostedService<MatchSchedulerService>();
builder.Services.AddScoped<IFantasyTeamService, FantasyTeamService>();
builder.Services.AddScoped<IFantasyTeamRepository, FantasyTeamRepository>();
builder.Services.Configure<ApiSportsOptions>(
    builder.Configuration.GetSection(ApiSportsOptions.SectionName));
builder.Services.AddHttpClient<INbaPlayersApiClient, ApiSportsClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSportsOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

var dbSettings = builder.Configuration.GetSection("DbSettings");
var connectionString = $"Host={dbSettings["Host"]};" +
                       $"Port={dbSettings["Port"]};" +
                       $"Database={dbSettings["Database"]};" +
                       $"Username={dbSettings["Username"]};" +
                       $"Password={dbSettings["Password"]};";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<FantasyLeagueHub>("/hubs/fantasy");

app.UseRouting();
app.MapGet("/health", () => "Fantasy League API is running!");

app.Run();

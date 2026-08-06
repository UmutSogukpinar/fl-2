using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.Common.Interfaces.ExternalServices;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.Services.Auth;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Domain.Entities.Auth;
using FantasyLeague.Infrastructure.Caching;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.ExternalServices.NbaApi;
using FantasyLeague.Infrastructure.Repositories.Drafts;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.Leagues;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using FantasyLeague.Infrastructure.Repositories.Users;
using FantasyLeague.Infrastructure.Security;
using FantasyLeague.WebApi.ExceptionHandlers;
using FantasyLeague.WebApi.Hubs;
using FantasyLeague.WebApi.Jobs.Drafts;
using FantasyLeague.WebApi.Jobs.Matches;
using FantasyLeague.WebApi.Jobs.NbaPlayers;
using FantasyLeague.WebApi.Middleware;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
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
builder.Services.AddScoped<DraftSchedulerJob>();
builder.Services.AddScoped<MatchSchedulerJob>();
builder.Services.AddScoped<NbaPlayerSyncJob>();
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

var jwtSection = builder.Configuration
    .GetRequiredSection("JwtTokenOptions");

var tokenOptions = jwtSection.Get<JwtTokenOptions>()
    ?? throw new InvalidOperationException(
        "JwtTokenOptions configuration is missing."
    );

builder.Services.Configure<JwtTokenOptions>(jwtSection);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Check issuer
                ValidateIssuer = true,
                ValidIssuer = tokenOptions.Issuer,

                // Check audience
                ValidateAudience = true,
                ValidAudience = tokenOptions.Audience,

                // Check life time of token
                ValidateLifetime = true,

                // Check signature
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(tokenOptions.Secret)
                ),

                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        options => options.UseNpgsqlConnection(connectionString))
    );

builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseHangfireDashboard("/hangfire");
}

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<DraftSchedulerJob>(
    "draft-scheduler",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Minutely);
recurringJobs.AddOrUpdate<MatchSchedulerJob>(
    "match-scheduler",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Minutely);
recurringJobs.AddOrUpdate<NbaPlayerSyncJob>(
    "nba-player-sync",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(1),
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")
    });

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FantasyLeagueHub>("/hubs/fantasy");

app.MapGet("/health", () => "Fantasy League API is running!")
    .AllowAnonymous();

app.Run();

using System.Text;
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
using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Infrastructure.Caching;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.ExternalServices.NbaApi;
using FantasyLeague.Infrastructure.Repositories.Drafts;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.Leagues;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using FantasyLeague.Infrastructure.Repositories.Users;
using FantasyLeague.Infrastructure.Security;
using FantasyLeague.WebApi.Authorization;
using FantasyLeague.WebApi.ExceptionHandlers;
using FantasyLeague.WebApi.Jobs.Drafts;
using FantasyLeague.WebApi.Jobs.Matches;
using FantasyLeague.WebApi.Jobs.NbaPlayers;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace FantasyLeague.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        return builder;
    }

    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddMemoryCache();
        services.AddSignalR();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<INbaPlayerSyncService, NbaPlayerSyncService>();
        services.AddScoped<INbaPlayerService, NbaPlayerService>();
        services.AddScoped<INbaPlayerRepository, NbaPlayerRepository>();
        services.AddScoped<ILeagueService, LeagueService>();
        services.AddScoped<ILeagueRepository, LeagueRepository>();
        services.AddScoped<ILeagueSetupRepository, LeagueSetupRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IDraftService, DraftService>();
        services.AddScoped<IFantasyTeamService, FantasyTeamService>();
        services.AddScoped<IFantasyTeamRepository, FantasyTeamRepository>();
        services.AddScoped<DraftSchedulerJob>();
        services.AddScoped<MatchSchedulerJob>();
        services.AddScoped<NbaPlayerSyncJob>();

        services.Configure<ApiSportsOptions>(
            configuration.GetSection(ApiSportsOptions.SectionName));
        services.AddHttpClient<INbaPlayersApiClient, ApiSportsClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ApiSportsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetRequiredSection("JwtTokenOptions");
        var tokenOptions = jwtSection.Get<JwtTokenOptions>()
            ?? throw new InvalidOperationException("JwtTokenOptions configuration is missing.");

        services.Configure<JwtTokenOptions>(jwtSection);
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = tokenOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = tokenOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(tokenOptions.Secret)),
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

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy(
                AuthorizationPolicies.AdminOnly,
                policy => policy.RequireRole(nameof(UserRole.Admin)));
        });

        return services;
    }

    public static IServiceCollection AddPersistenceAndBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.BuildPostgreSqlConnectionString();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHangfire(configuration => configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }

    private static string BuildPostgreSqlConnectionString(this IConfiguration configuration)
    {
        var settings = configuration.GetRequiredSection("DbSettings");
        return $"Host={settings["Host"]};" +
               $"Port={settings["Port"]};" +
               $"Database={settings["Database"]};" +
               $"Username={settings["Username"]};" +
               $"Password={settings["Password"]};";
    }
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using FantasyLeague.Application.Services.Auth;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.WebApi.Controllers.Auth;
using FantasyLeague.WebApi.ExceptionHandlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class ApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _application;

    private ApiTestHost(
        WebApplication application,
        Mock<IUserService> users,
        Mock<IAuthService> auth)
    {
        _application = application;
        Users = users;
        Auth = auth;
    }

    public HttpClient Client { get; private set; } = null!;
    public Mock<IUserService> Users { get; }
    public Mock<IAuthService> Auth { get; }

    public static async Task<ApiTestHost> CreateAsync(Action<ApiTestHost>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        var users = new Mock<IUserService>();
        var auth = new Mock<IAuthService>();
        builder.Services.AddSingleton(users.Object);
        builder.Services.AddSingleton(auth.Object);
        builder.Services.AddSingleton(Mock.Of<IDraftService>());
        builder.Services.AddSingleton(Mock.Of<IFantasyTeamService>());
        builder.Services.AddSingleton(Mock.Of<ILeagueService>());
        builder.Services.AddSingleton(Mock.Of<INbaPlayerService>());
        builder.Services.AddSingleton(Mock.Of<INbaPlayerSyncService>());
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build();
        });

        var application = builder.Build();
        application.UseExceptionHandler();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapControllers();

        var result = new ApiTestHost(application, users, auth);
        configure?.Invoke(result);
        await application.StartAsync();
        result.Client = application.GetTestClient();
        return result;
    }

    public HttpRequestMessage Request(HttpMethod method, string path, bool authenticated = false)
    {
        var request = new HttpRequestMessage(method, path);
        if (authenticated)
            request.Headers.Add(TestAuthenticationHandler.Header, "integration-user");
        return request;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _application.DisposeAsync();
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";
    public const string Header = "X-Test-Auth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Header))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "integration-user")], SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}

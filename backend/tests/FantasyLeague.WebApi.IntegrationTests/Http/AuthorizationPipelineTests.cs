using System.Net;
using System.Net.Http.Json;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class AuthorizationPipelineTests
{
    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "GET", "/api/users" },
        { "GET", $"/api/users/{Guid.NewGuid()}" },
        { "PUT", $"/api/users/{Guid.NewGuid()}" },
        { "DELETE", $"/api/users/{Guid.NewGuid()}" },
        { "GET", "/api/leagues" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}" },
        { "POST", "/api/leagues" },
        { "PUT", $"/api/leagues/{Guid.NewGuid()}" },
        { "DELETE", $"/api/leagues/{Guid.NewGuid()}" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}/standings" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}/fixtures" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}/draft-order" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}/members" },
        { "GET", "/api/fantasy-teams" },
        { "GET", $"/api/fantasy-teams/{Guid.NewGuid()}" },
        { "GET", $"/api/fantasy-teams/{Guid.NewGuid()}/players" },
        { "GET", $"/api/fantasy-teams/{Guid.NewGuid()}/player-pool" },
        { "GET", $"/api/fantasy-teams/{Guid.NewGuid()}/transfers" },
        { "POST", $"/api/fantasy-teams/{Guid.NewGuid()}/transfers" },
        { "GET", "/api/nba-players" },
        { "GET", "/api/nba-players/search" },
        { "GET", $"/api/nba-players/{Guid.NewGuid()}" },
        { "POST", "/api/nba-players/sync" },
        { "GET", $"/api/leagues/{Guid.NewGuid()}/draft" },
        { "POST", $"/api/leagues/{Guid.NewGuid()}/draft/close" },
        { "POST", $"/api/leagues/{Guid.NewGuid()}/draft/picks" }
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutAuthentication_ReturnsUnauthorized(
        string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/auth/sign-in")]
    [InlineData("POST", "/api/auth/refresh")]
    [InlineData("POST", "/api/auth/sign-out")]
    [InlineData("POST", "/api/users")]
    public async Task AnonymousEndpoint_IsNotBlockedByAuthorization(string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var request = host.Request(new HttpMethod(method), path);
        request.Content = JsonContent.Create(new { });
        var response = await host.Client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users/not-a-guid")]
    [InlineData("/api/fantasy-teams/not-a-guid")]
    [InlineData("/api/nba-players/not-a-guid")]
    [InlineData("/api/leagues/not-a-guid/draft")]
    public async Task GuidRoute_WithMalformedIdentifier_ReturnsNotFound(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, path, authenticated: true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

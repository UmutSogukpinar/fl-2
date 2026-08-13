using System.Net;
using System.Net.Http.Json;
using System.Text;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Users;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class HttpEdgeCaseIntegrationTests
{
    [Theory]
    [InlineData("POST", "/api/auth/sign-in")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/users/33333333-3333-3333-3333-333333333333")]
    [InlineData("POST", "/api/leagues")]
    [InlineData("POST", "/api/fantasy-teams/33333333-3333-3333-3333-333333333333/transfers")]
    public async Task BodyEndpoint_WithEmptyJsonBody_ReturnsBadRequest(
        string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();
        using var request = host.Request(new HttpMethod(method), path, authenticated: true);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/leagues")]
    public async Task JsonEndpoint_WithTextContentType_ReturnsUnsupportedMediaType(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();
        using var request = host.Request(HttpMethod.Post, path, authenticated: true);
        request.Content = new StringContent("{}", Encoding.UTF8, "text/plain");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/nba-players?pageNumber=0")]
    [InlineData("/api/nba-players?pageSize=101")]
    [InlineData("/api/nba-players/search?pageSize=-1")]
    [InlineData("/api/fantasy-teams?leagueId=33333333-3333-3333-3333-333333333333&pageNumber=0")]
    [InlineData("/api/fantasy-teams/33333333-3333-3333-3333-333333333333/player-pool?pageSize=0")]
    [InlineData("/api/leagues?pageSize=2147483647")]
    public async Task PaginatedEndpoint_WithOutOfRangeValue_ReturnsBadRequest(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, path, authenticated: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/nba-players/33333333-3333-3333-3333-333333333333?size=unknown")]
    [InlineData("/api/nba-players/search?size=999")]
    [InlineData("/api/leagues?status=not-a-status")]
    public async Task Query_WithInvalidEnum_ReturnsBadRequest(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, path, authenticated: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/fantasy-teams?leagueId=invalid")]
    [InlineData("/api/nba-players/search?id=invalid")]
    public async Task Query_WithMalformedGuid_ReturnsBadRequest(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, path, authenticated: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("PATCH", "/api/users/33333333-3333-3333-3333-333333333333")]
    [InlineData("DELETE", "/api/auth/sign-in")]
    [InlineData("PUT", "/api/nba-players")]
    public async Task ExistingRoute_WithUnsupportedMethod_ReturnsMethodNotAllowed(
        string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(new HttpMethod(method), path, authenticated: true));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WhenCredentialsRejected_ReturnsProblemWithoutCookies()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.SignInAsync(
                    It.IsAny<SignInRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("Invalid credentials.")));

        var response = await host.Client.PostAsJsonAsync(
            "/api/auth/sign-in", new SignInRequest("unknown", "wrong"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid credentials.", problem?.Detail);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_WhenTokenRejected_DoesNotRotateCookies()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.RefreshAsync(
                    "expired-token", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("Refresh token expired.")));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "access_token=ignored; refresh_token=expired-token");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_WithMultipleCookieNames_UsesOnlyRefreshToken()
    {
        var user = new Application.DTOs.Responses.Users.UserResponse(
            Guid.NewGuid(), "cookie-user", "cookie@example.com", DateTime.UtcNow, null);
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.RefreshAsync(
                    "selected-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync((user, "access", "refresh")));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie",
            "access_token=must-not-be-used; unrelated=value; refresh_token=selected-token");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Auth.Verify(service => service.RefreshAsync(
            "selected-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnexpectedException_DoesNotExposeInternalMessageOrStackTrace()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        const string secret = "database-password=do-not-expose";
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetByIdAsync(
                    id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(secret)));

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, $"/api/users/{id}", authenticated: true));
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(secret, payload);
        Assert.DoesNotContain(nameof(InvalidOperationException), payload);
        Assert.DoesNotContain("stack", payload, StringComparison.OrdinalIgnoreCase);
    }
}

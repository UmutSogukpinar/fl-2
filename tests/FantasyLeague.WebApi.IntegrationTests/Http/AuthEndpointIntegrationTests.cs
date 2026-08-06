using System.Net;
using System.Net.Http.Json;
using System.Text;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class AuthEndpointIntegrationTests
{
    private static readonly UserResponse User = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "api-user", "api@example.com", DateTime.UtcNow, null);

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsUserAndSecureCookies()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.SignInAsync(
                    It.IsAny<SignInRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User, "access-value", "refresh-value")));

        var response = await host.Client.PostAsJsonAsync(
            "/api/auth/sign-in", new SignInRequest("api-user", "Password1!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(User.Id, (await response.Content.ReadFromJsonAsync<UserResponse>())?.Id);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, value => value.Contains("access_token=access-value"));
        Assert.Contains(cookies, value => value.Contains("refresh_token=refresh-value"));
        Assert.All(cookies, value => Assert.Contains("httponly", value, StringComparison.OrdinalIgnoreCase));
        Assert.All(cookies, value => Assert.Contains("secure", value, StringComparison.OrdinalIgnoreCase));
        Assert.All(cookies, value => Assert.Contains("samesite=strict", value, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_WithCookie_ForwardsTokenAndRotatesCookies()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.RefreshAsync(
                    "old-refresh", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User, "new-access", "new-refresh")));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=old-refresh");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Auth.Verify(service => service.RefreshAsync(
            "old-refresh", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            value => value.Contains("refresh_token=new-refresh"));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ForwardsEmptyToken()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Auth.Setup(service => service.RefreshAsync(
                    string.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User, "access", "refresh")));

        var response = await host.Client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.Auth.Verify(service => service.RefreshAsync(
            string.Empty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOut_WithRefreshCookie_RevokesTokenAndExpiresCookies()
    {
        await using var host = await ApiTestHost.CreateAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/sign-out");
        request.Headers.Add("Cookie", "refresh_token=logout-token");

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        host.Auth.Verify(service => service.SignOutAsync(
            "logout-token", It.IsAny<CancellationToken>()), Times.Once);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, value => value.StartsWith("access_token="));
        Assert.Contains(cookies, value => value.StartsWith("refresh_token="));
    }

    [Fact]
    public async Task SignOut_WithoutRefreshCookie_ForwardsEmptyToken()
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.PostAsync("/api/auth/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        host.Auth.Verify(service => service.SignOutAsync(
            string.Empty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignIn_WithMalformedJson_ReturnsBadRequest()
    {
        await using var host = await ApiTestHost.CreateAsync();
        using var content = new StringContent("{ invalid", Encoding.UTF8, "application/json");

        var response = await host.Client.PostAsync("/api/auth/sign-in", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Auth.Verify(service => service.SignInAsync(
            It.IsAny<SignInRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

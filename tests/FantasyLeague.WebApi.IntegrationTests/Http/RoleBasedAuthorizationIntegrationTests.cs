using System.Net;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Domain.Entities.Users;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class RoleBasedAuthorizationIntegrationTests
{
    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/users" },
        { "DELETE", $"/api/users/{Guid.NewGuid()}" },
        { "POST", "/api/nba-players/sync" }
    };

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_WithUserRole_ReturnsForbidden(string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(host.Request(
            new HttpMethod(method), path, authenticated: true, role: nameof(UserRole.User)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_WithoutAuthentication_ReturnsUnauthorized(
        string method, string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_ReturnsSuccess()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetAsync(
                    It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResponse<UserResponse>([], 1, 10, 0, 0)));

        var response = await host.Client.SendAsync(host.Request(
            HttpMethod.Get, "/api/users", authenticated: true, role: nameof(UserRole.Admin)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithAdminRole_ExecutesOperation()
    {
        var userId = Guid.NewGuid();
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(host.Request(
            HttpMethod.Delete, $"/api/users/{userId}",
            authenticated: true, role: nameof(UserRole.Admin)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        host.Users.Verify(service => service.DeleteAsync(
            userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NbaSync_WithAdminRole_ExecutesOperation()
    {
        var result = new NbaPlayerSyncResponse(2026, 2, 3, 0, 5);
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.NbaSync.Setup(service => service.SyncActivePlayersAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result));

        var response = await host.Client.SendAsync(host.Request(
            HttpMethod.Post, "/api/nba-players/sync",
            authenticated: true, role: nameof(UserRole.Admin)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        host.NbaSync.Verify(service => service.SyncActivePlayersAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NonAdminEndpoint_WithUserRole_RemainsAccessible()
    {
        var userId = Guid.NewGuid();
        var user = new UserResponse(
            userId, "regular-user", "regular@example.com", DateTime.UtcNow, null);
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetByIdAsync(
                    userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user));

        var response = await host.Client.SendAsync(host.Request(
            HttpMethod.Get, $"/api/users/{userId}",
            authenticated: true, role: nameof(UserRole.User)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

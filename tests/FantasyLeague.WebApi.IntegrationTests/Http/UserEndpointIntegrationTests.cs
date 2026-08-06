using System.Net;
using System.Net.Http.Json;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Http;

public sealed class UserEndpointIntegrationTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly UserResponse User = new(
        UserId, "http-user", "http@example.com", DateTime.UtcNow, null, "Europe/Istanbul");

    [Fact]
    public async Task GetUsers_WhenAuthenticated_ReturnsPagedJsonResponse()
    {
        var page = new PagedResponse<UserResponse>([User], 1, 10, 1, 1);
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetAsync(
                    It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(page));
        using var request = host.Request(HttpMethod.Get, "/api/users?pageNumber=1&pageSize=10", true);

        var response = await host.Client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<UserResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body?.TotalCount);
        Assert.Equal(UserId, Assert.Single(body!.Items).Id);
    }

    [Fact]
    public async Task GetUserById_WhenAuthenticated_ReturnsRequestedUser()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetByIdAsync(
                    UserId, It.IsAny<CancellationToken>())).ReturnsAsync(User));

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, $"/api/users/{UserId}", true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(UserId, (await response.Content.ReadFromJsonAsync<UserResponse>())?.Id);
    }

    [Fact]
    public async Task CreateUser_AsAnonymous_ReturnsCreatedAndLocationHeader()
    {
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.CreateAsync(
                    It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(User));

        var response = await host.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("http-user", "http@example.com", "Password1!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(UserId.ToString(), response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task UpdateUser_WhenAuthenticated_ReturnsUpdatedUser()
    {
        var updated = User with { Username = "updated-user" };
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.UpdateAsync(
                    UserId, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated));
        using var request = host.Request(HttpMethod.Put, $"/api/users/{UserId}", true);
        request.Content = JsonContent.Create(new UpdateUserRequest("updated-user", User.Email));

        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("updated-user",
            (await response.Content.ReadFromJsonAsync<UserResponse>())?.Username);
    }

    [Fact]
    public async Task DeleteUser_WhenAuthenticated_ReturnsNoContent()
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Delete, $"/api/users/{UserId}", true));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        host.Users.Verify(service => service.DeleteAsync(
            UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("bad-request", 400, "Bad request")]
    [InlineData("unauthorized", 401, "Unauthorized")]
    [InlineData("forbidden", 403, "Forbidden")]
    [InlineData("not-found", 404, "Resource not found")]
    [InlineData("conflict", 409, "Conflict")]
    [InlineData("external", 502, "External service error")]
    [InlineData("unexpected", 500, "An unexpected error occurred")]
    public async Task ApplicationException_IsMappedToProblemDetails(
        string exceptionType, int expectedStatus, string expectedTitle)
    {
        var exception = CreateException(exceptionType);
        await using var host = await ApiTestHost.CreateAsync(api =>
            api.Users.Setup(service => service.GetByIdAsync(
                    UserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception));

        var response = await host.Client.SendAsync(
            host.Request(HttpMethod.Get, $"/api/users/{UserId}", true));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal(expectedTitle, problem?.Title);
        Assert.Equal($"/api/users/{UserId}", problem?.Instance);
        Assert.True(problem?.Extensions.ContainsKey("traceId"));
        if (expectedStatus == 500)
            Assert.Equal("The server was unable to complete the request.", problem?.Detail);
        else
            Assert.Equal("integration failure", problem?.Detail);
    }

    [Theory]
    [InlineData("/api/users?pageNumber=0")]
    [InlineData("/api/users?pageSize=0")]
    [InlineData("/api/users?pageSize=101")]
    public async Task GetUsers_WithInvalidPagination_ReturnsValidationProblem(string path)
    {
        await using var host = await ApiTestHost.CreateAsync();

        var response = await host.Client.SendAsync(host.Request(HttpMethod.Get, path, true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Users.Verify(service => service.GetAsync(
            It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Exception CreateException(string type) => type switch
    {
        "bad-request" => new BadRequestException("integration failure"),
        "unauthorized" => new UnauthorizedException("integration failure"),
        "forbidden" => new ForbiddenException("integration failure"),
        "not-found" => new NotFoundException("integration failure"),
        "conflict" => new ConflictException("integration failure"),
        "external" => new ExternalServiceException("integration failure"),
        _ => new InvalidOperationException("sensitive internal message")
    };
}

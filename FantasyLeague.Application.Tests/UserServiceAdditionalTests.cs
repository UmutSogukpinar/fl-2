using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests;

public sealed class UserServiceAdditionalTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly UserService _service;

    public UserServiceAdditionalTests()
    {
        _service = new UserService(_repository.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsCorrectPaginationMetadata()
    {
        var request = new PaginationRequest { PageNumber = 2, PageSize = 3 };
        var users = new[]
        {
            new UserResponse(Guid.NewGuid(), "user", "user@example.com", DateTime.UtcNow, null)
        };
        _repository
            .Setup(repository => repository.GetPagedAsync(
                2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 8));

        var response = await _service.GetAsync(request);

        Assert.Same(users, response.Items);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(3, response.PageSize);
        Assert.Equal(8, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameOrEmailExists_ThrowsConflictException()
    {
        var request = new CreateUserRequest(
            "  ExistingUser  ", "  EXISTING@EXAMPLE.COM  ", "password123");
        _repository
            .Setup(repository => repository.ExistsAsync(
                "ExistingUser",
                "existing@example.com",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));

        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _repository.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SignInAsync_WithValidCredentials_ReturnsUserResponse()
    {
        var user = CreateUser();
        var request = new SignInRequest(user.Email, "password123");
        _repository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(hasher => hasher.Verify(request.Password, user.Password))
            .Returns(true);

        var response = await _service.SignInAsync(request);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
    }

    [Fact]
    public async Task SignInAsync_WhenUserDoesNotExist_ThrowsUnauthorizedWithoutVerifyingHash()
    {
        var request = new SignInRequest("missing@example.com", "password123");
        _repository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.SignInAsync(request));

        _passwordHasher.Verify(
            hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SignInAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var user = CreateUser();
        var request = new SignInRequest(user.Email, "wrong-password");
        _repository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(hasher => hasher.Verify(request.Password, user.Password))
            .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.SignInAsync(request));
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "user",
        Email = "user@example.com",
        Password = "hashed-password"
    };
}

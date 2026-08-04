using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests.Services.Users;

public sealed class UserServiceAdditionalTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly UserService _service;

    public UserServiceAdditionalTests()
    {
        _service = new UserService(_repository.Object, _passwordHasher.Object);
    }

    // Case: Get
    // Reasoning: This test verifies the Get operation.
    // Expected Result: The expected outcome is: Returns Correct Pagination Metadata.
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
                It.Is<PaginationRequest>(request =>
                    request.PageNumber == 2 && request.PageSize == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 8));

        var response = await _service.GetAsync(request);

        Assert.Same(users, response.Items);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(3, response.PageSize);
        Assert.Equal(8, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
    }

    // Case: Get when With Invalid Pagination
    // Reasoning: This test verifies Get under the With Invalid Pagination condition.
    // Expected Result: The expected outcome is: Does Not Query Repository.
    [Fact]
    public async Task GetAsync_WithInvalidPagination_DoesNotQueryRepository()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetAsync(new PaginationRequest { PageSize = 0 }));

        _repository.Verify(repository => repository.GetPagedAsync(
            It.IsAny<PaginationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Get when With Null Request
    // Reasoning: This test verifies Get under the With Null Request condition.
    // Expected Result: The expected outcome is: Does Not Query Repository.
    [Fact]
    public async Task GetAsync_WithNullRequest_DoesNotQueryRepository()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetAsync(null!));

        _repository.Verify(repository => repository.GetPagedAsync(
            It.IsAny<PaginationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Create when Username Or Email Exists
    // Reasoning: This test verifies Create under the Username Or Email Exists condition.
    // Expected Result: The expected outcome is: Throws Conflict Exception.
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

    // Case: Sign In when With Valid Credentials
    // Reasoning: This test verifies Sign In under the With Valid Credentials condition.
    // Expected Result: The expected outcome is: Returns User Response.
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

    // Case: Create when With Null Request
    // Reasoning: This test verifies Create under the With Null Request condition.
    // Expected Result: The expected outcome is: Does Not Hash Or Persist.
    [Fact]
    public async Task CreateAsync_WithNullRequest_DoesNotHashOrPersist()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(null!));

        _passwordHasher.Verify(
            hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _repository.Verify(
            repository => repository.Add(It.IsAny<User>()), Times.Never);
    }

    // Case: Sign In when With Invalid Email
    // Reasoning: This test verifies Sign In under the With Invalid Email condition.
    // Expected Result: The expected outcome is: Does Not Query Repository.
    [Fact]
    public async Task SignInAsync_WithInvalidEmail_DoesNotQueryRepository()
    {
        var request = new SignInRequest("invalid-email", "password123");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.SignInAsync(request));

        _repository.Verify(repository => repository.GetByEmailAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Sign In when User Does Not Exist
    // Reasoning: This test verifies Sign In under the User Does Not Exist condition.
    // Expected Result: The expected outcome is: Throws Unauthorized Without Verifying Hash.
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

    // Case: Sign In when With Wrong Password
    // Reasoning: This test verifies Sign In under the With Wrong Password condition.
    // Expected Result: The expected outcome is: Throws Unauthorized Exception.
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

namespace FantasyLeague.Application.Tests;

using Moq;
using Xunit;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Domain.Entities;

public class UserServiceTests
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _repositoryMock = new Mock<IUserRepository>();
        _service = new UserService(_repositoryMock.Object, _passwordHasherMock.Object);
    }

    // ====================== Get User Tests ======================

    // Case: User found by ID
    // Reasoning: When the user is found by ID,
    [Fact]
    public async Task DoesFindUserById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = CreateUserResponse(userId);

        _repositoryMock
            .Setup(s => s.GetResponseByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var actualUser = await _service.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(actualUser);
        Assert.Equal(expectedUser.Id, actualUser.Id);
        Assert.Equal(expectedUser.Email, actualUser.Email);
    }


    // Case: User not found by ID
    // Reasoning: When the user is not found by ID,
    // the service should throw a NotFoundException
    [Fact]
    public async Task DoesNotFindUserById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(s => s.GetResponseByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _service.GetByIdAsync(userId, CancellationToken.None);
        });
    }

    // ====================== Create User Tests ======================

    // Case: Create user
    // Reasoning: When a user is created,
    // the service should return the created user
    [Fact]
    public async Task DoesCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Umut",
            "us.example@.com",
            "passwordffffff"
        );

        _repositoryMock.Setup(s => s.Add(
            It.IsAny<User>()))
            .Returns((User user) => user);

        // Act
        var createdUser = await _service.CreateAsync(
            request, CancellationToken.None
        );

        // Assert
        Assert.NotNull(createdUser);
        Assert.Equal(request.Username, createdUser.Username);
        Assert.Equal(request.Email, createdUser.Email);
    }

    // Case: Create user with empty email
    // Reasoning: When a user is created with an empty email,
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithEmptyEmail()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Umut",
            "",
            "password"
        );
        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with invalid email
    // Reasoning: When a user is created with an invalid email,
    // the service should throw an ArgumentException
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithInvalidEmail()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Umut",
            "invalid-email",
            "password"
        );
        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with invalid password
    // Reasoning: When a user is created with an invalid password,
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithInvalidPassword()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Umut",
            "us.example.com",
            ""
        );
        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with short password
    // Reasoning: When a user is created with a short password,
    [Fact]
    public void DoesThrownExceptionWhenCreatingUserWithShortPassword()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Umut",
            "us.example@.com",
            "pass"
        );
        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with long password
    // Reasoning: When a user is created with a long password,
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithLongPassword()
    {
        // Arrange
        var longPassword = new string('a', 129); // 129 characters
        var request = new CreateUserRequest(
            "Umut",
            "us.example@.com",
            longPassword
        );

        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with empty username
    // Reasoning: When a user is created with an empty username,
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithEmptyUsername()
    {
        // Arrange
        var request = new CreateUserRequest(
            "",
            "us.example@.com",
            "password"
        );
        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with short username
    // Reasoning: When a user is created with a short username,
    [Fact]
    public void DoesThrowExceptionWhenCreatingUserWithShortUsername()
    {
        // Arrange
        var request = new CreateUserRequest(
            "Inv",
            "us.example@.com",
            "password"
        );

        // Act & Assert
        Assert.Throws<BadRequestException>(() =>
        {
            _service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    // ====================== Delete User Tests ======================

    // Case: Delete user
    // Reasoning: When a user is deleted,
    [Fact]
    public async Task DoesDeleteUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = CreateUser(userId);
        _repositoryMock
            .Setup(s => s.GetTrackedByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        await _service.DeleteAsync(userId, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(s => s.Remove(expectedUser), Times.Once);
        _repositoryMock.Verify(s => s.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once
        );
    }

    // Case: Delete non-existent user
    // Reasoning: When a user is deleted that does not exist,
    [Fact]
    public async Task DoesThrowExceptionWhenDeletingNonExistentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(s => s.GetTrackedByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () => {
            await _service.DeleteAsync(userId, CancellationToken.None);
        });
    }


    // ====================== Update User Tests ======================

    // Case: Update user
    // Reasoning: When a user is updated,
    [Fact]
    public async Task DoesUpdateUser()
    {
        // Arrange
        var updatedUserName = "UpdatedUserName";
        var updateUserEmail = "updated.email@example.com";

        var userId = Guid.NewGuid();
        var existingUser = CreateUser(userId);
        _repositoryMock.Setup(s => s.GetTrackedByIdAsync(
            userId,
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(existingUser);

        var updateRequest = new UpdateUserRequest(
            updatedUserName,
            updateUserEmail
        );

        // Act
        var result = await _service.UpdateAsync(
            userId, updateRequest, CancellationToken.None
        );

        // Assert
        Assert.Equal(updatedUserName, result.Username);
        Assert.Equal(updateUserEmail, result.Email);
    }

    // Case: Update non-existent user
    // Reasoning: When a user is updated that does not exist,
    // the service should throw a NotFoundException
    [Fact]
    public async Task DoesThrowExceptionWhenUpdatingNonExistentUser()
    {
        // Arrange
        var updatedUserName = "UpdatedUserName";
        var updateUserEmail = "updated.email@example.com";
        var updateRequest = new UpdateUserRequest(
            updatedUserName,
            updateUserEmail
        );

        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _service.UpdateAsync(
                userId, updateRequest, CancellationToken.None
            );
        });
    }

    // Case: Update user with invalid email
    // Reasoning: When a user is updated with an invalid email,
    // the service should throw a BadRequestException
    [Fact]
    public async Task DoesThrowExceptionWhenUpdatingUserWithInvalidEmail()
    {
        // Arrange
        var updatedUserName = "UpdatedUserName";
        var invalidEmail = "invalid-email";

        var userId = Guid.NewGuid();
        var existingUser = CreateUser(userId);
        _repositoryMock.Setup(s => s.GetTrackedByIdAsync(
            userId,
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(existingUser);

        var updateRequest = new UpdateUserRequest(
            updatedUserName,
            invalidEmail
        );

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _service.UpdateAsync(userId, updateRequest, CancellationToken.None);
        });
    }

    // Case: Update user with empty username
    // Reasoning: When a user is updated with an empty username,
    // the service should throw a BadRequestException
    [Fact]
    public async Task DoesThrowExceptionWhenUpdatingWithEmptyUsername()
    {
        // Arrange
        var updatedUserName = "";
        var updateUserEmail = "updated.email@example.com";

        var userId = Guid.NewGuid();
        var existingUser = CreateUser(userId);

        var updateRequest = new UpdateUserRequest(
            updatedUserName,
            updateUserEmail
        );

        _repositoryMock.Setup(s => s.GetTrackedByIdAsync(
                userId,
                It.IsAny<CancellationToken>())
            ).ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _service.UpdateAsync(userId, updateRequest, CancellationToken.None);
        });
    }


    // Case: Update user with empty username
    // Reasoning: When a user is updated with an empty username,
    // the service should throw a BadRequestException
    [Fact]
    public async Task DoesThrowExceptionWhenUpdatingWithInvalidUsername()
    {
        // Arrange
        var updatedUserName = "   "; // Username with only spaces
        var updateUserEmail = "updated.email@example.com";

        var userId = Guid.NewGuid();
        var existingUser = CreateUser(userId);

        var updateRequest = new UpdateUserRequest(
            updatedUserName,
            updateUserEmail
        );

        _repositoryMock.Setup(s => s.GetTrackedByIdAsync(
                userId,
                It.IsAny<CancellationToken>())
            ).ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _service.UpdateAsync(userId, updateRequest, CancellationToken.None);
        });
    }

    private static User CreateUser(Guid id)
    {
        var expectedUser = new User
        {
            Id = id,
            Username = "Umut",
            Email = "us.example.com",
            Password = "password",
        };

        return expectedUser;
    }

    private static UserResponse CreateUserResponse(Guid id) => new(
        id,
        "Umut",
        "us.example.com",
        DateTime.UtcNow,
        null);
}

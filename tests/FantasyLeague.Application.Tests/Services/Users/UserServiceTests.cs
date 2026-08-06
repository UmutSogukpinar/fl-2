using FantasyLeague.Domain.Entities.Users;

namespace FantasyLeague.Application.Tests.Services.Users;

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
        _service = new UserService(
            _repositoryMock.Object,
            _passwordHasherMock.Object);
    }

    // ====================== Get User Tests ======================

    // Case: Does Find User By Id
    // Reasoning: This test verifies the Does Find User By Id scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
        var actualUser = await _service.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(actualUser);
        Assert.Equal(expectedUser.Id, actualUser.Id);
        Assert.Equal(expectedUser.Email, actualUser.Email);
    }


    // Case: User not found by ID
    // Reasoning: When the user is not found by ID,
    // the service should throw a NotFoundException
    // Expected Result: The behavior should match the assertions defined for this case.
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
            await _service.GetByIdAsync(userId);
        });
    }

    // ====================== Create User Tests ======================

    // Case: Create user
    // Reasoning: When a user is created,
    // the service should return the created user
    // Expected Result: The behavior should match the assertions defined for this case.
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
            request
        );

        // Assert
        Assert.NotNull(createdUser);
        Assert.Equal(request.Username, createdUser.Username);
        Assert.Equal(request.Email, createdUser.Email);
    }

    // Case: Does Throw Exception When Creating User With Empty Email
    // Reasoning: This test verifies the Does Throw Exception When Creating User With Empty Email scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Create user with invalid email
    // Reasoning: When a user is created with an invalid email,
    // the service should throw an ArgumentException
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Does Throw Exception When Creating User With Invalid Password
    // Reasoning: This test verifies the Does Throw Exception When Creating User With Invalid Password scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Does Thrown Exception When Creating User With Short Password
    // Reasoning: This test verifies the Does Thrown Exception When Creating User With Short Password scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Does Throw Exception When Creating User With Long Password
    // Reasoning: This test verifies the Does Throw Exception When Creating User With Long Password scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Does Throw Exception When Creating User With Empty Username
    // Reasoning: This test verifies the Does Throw Exception When Creating User With Empty Username scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    // Case: Does Throw Exception When Creating User With Short Username
    // Reasoning: This test verifies the Does Throw Exception When Creating User With Short Username scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            _service.CreateAsync(request).GetAwaiter().GetResult();
        });
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameExceedsMaximumLength_ThrowsBadRequestException()
    {
        var request = new CreateUserRequest(
            new string('u', 51),
            "user@example.com",
            "password"
        );

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(request));

        Assert.Equal("Username cannot exceed 50 characters.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailExceedsMaximumLength_ThrowsBadRequestException()
    {
        var request = new CreateUserRequest(
            "valid-user",
            $"{new string('a', 243)}@example.com",
            "password"
        );

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(request));

        Assert.Equal("Email cannot exceed 254 characters.", exception.Message);
    }

    // ====================== Delete User Tests ======================

    // Case: Does Delete User
    // Reasoning: This test verifies the Does Delete User scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
        await _service.DeleteAsync(userId);

        // Assert
        _repositoryMock.Verify(s => s.Remove(expectedUser), Times.Once);
        _repositoryMock.Verify(s => s.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once
        );
    }

    // Case: Does Throw Exception When Deleting Non Existent User
    // Reasoning: This test verifies the Does Throw Exception When Deleting Non Existent User scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _service.DeleteAsync(userId);
        });
    }


    // ====================== Update User Tests ======================

    // Case: Does Update User
    // Reasoning: This test verifies the Does Update User scenario.
    // Expected Result: The behavior should match the assertions defined for this case.
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
            userId, updateRequest
        );

        // Assert
        Assert.Equal(updatedUserName, result.Username);
        Assert.Equal(updateUserEmail, result.Email);
    }

    // Case: Update non-existent user
    // Reasoning: When a user is updated that does not exist,
    // the service should throw a NotFoundException
    // Expected Result: The behavior should match the assertions defined for this case.
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
                userId, updateRequest
            );
        });
    }

    // Case: Update user with invalid email
    // Reasoning: When a user is updated with an invalid email,
    // the service should throw a BadRequestException
    // Expected Result: The behavior should match the assertions defined for this case.
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
            await _service.UpdateAsync(userId, updateRequest);
        });
    }

    // Case: Update user with empty username
    // Reasoning: When a user is updated with an empty username,
    // the service should throw a BadRequestException
    // Expected Result: The behavior should match the assertions defined for this case.
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
            await _service.UpdateAsync(userId, updateRequest);
        });
    }


    // Case: Update user with empty username
    // Reasoning: When a user is updated with an empty username,
    // the service should throw a BadRequestException
    // Expected Result: The behavior should match the assertions defined for this case.
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
            await _service.UpdateAsync(userId, updateRequest);
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

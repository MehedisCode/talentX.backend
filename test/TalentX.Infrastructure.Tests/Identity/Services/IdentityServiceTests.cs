using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Identity.Services;

/// <summary>
/// Unit tests for IdentityService
/// Tests cover user creation, password validation, email management, and user deletion
/// Uses AAA (Arrange-Act-Assert) pattern with mocked Identity components
/// </summary>
public class IdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly IdentityService _identityService;

    public IdentityServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            new Mock<IRoleStore<IdentityRole>>().Object,
            null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();

        _identityService = new IdentityService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object);
    }

    #region CreateApplicationUserAsync - Happy Path

    [Fact]
    public async Task CreateApplicationUserAsync_WithValidParameters_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var email = "newuser@example.com";
        var password = "ValidPassword123!";
        var role = UserRole.Employer;

        var identityResult = IdentityResult.Success;

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(identityResult);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().NotBeNullOrEmpty();

        _roleManagerMock.Verify(x => x.RoleExistsAsync(role.ToString()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()), Times.Once);
    }

    [Theory]
    [InlineData(UserRole.Employer)]
    [InlineData(UserRole.Talent)]
    public async Task CreateApplicationUserAsync_WithDifferentRoles_ShouldAssignCorrectRole(UserRole role)
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeTrue();
        _userManagerMock.Verify(
            x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()),
            Times.Once);
    }

    #endregion

    #region CreateApplicationUserAsync - Error Conditions

    [Fact]
    public async Task CreateApplicationUserAsync_WithInvalidRole_ShouldReturnFailure()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";
        var role = UserRole.Employer;

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(false);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid user role.");
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateApplicationUserAsync_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var email = "existing@example.com";
        var password = "Password123!";
        var role = UserRole.Employer;

        var existingUser = new ApplicationUser { Email = email };

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Email is already in use.");
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateApplicationUserAsync_WhenUserCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var email = "user@example.com";
        var password = "WeakPassword";
        var role = UserRole.Employer;

        var identityError = new IdentityError { Description = "Password too weak." };
        var createResult = IdentityResult.Failed(identityError);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(createResult);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Password too weak.");
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateApplicationUserAsync_WhenRoleAssignmentFails_ShouldDeleteUserAndReturnFailure()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";
        var role = UserRole.Employer;

        var user = new ApplicationUser { Id = "user-id", Email = email };
        var roleError = new IdentityError { Description = "Failed to assign role." };

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(role.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .Callback<ApplicationUser, string>((u, p) => u.Id = user.Id)
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()))
            .ReturnsAsync(IdentityResult.Failed(roleError));

        _userManagerMock
            .Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.CreateApplicationUserAsync(email, password, role);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Failed to assign role.");
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region ValidatePasswordAsync - Happy Path

    [Fact]
    public async Task ValidatePasswordAsync_WithValidCredentials_ShouldReturnSuccessWithUserData()
    {
        // Arrange
        var email = "user@example.com";
        var password = "ValidPassword123!";
        var userId = "user-id";

        var user = new ApplicationUser { Id = userId, Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.Email.Should().Be(email);
        result.Data.Roles.Should().Contain("User");

        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithCorrectPassword_ShouldResetAccessFailedCount()
    {
        // Arrange
        var email = "user@example.com";
        var password = "ValidPassword123!";
        var userId = "user-id";

        var user = new ApplicationUser { Id = userId, Email = email, AccessFailedCount = 3 };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    #endregion

    #region ValidatePasswordAsync - Error Conditions

    [Fact]
    public async Task ValidatePasswordAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid credentials.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithLockedOutAccount_ShouldReturnFailure()
    {
        // Arrange
        var email = "locked@example.com";
        var password = "Password123!";

        var user = new ApplicationUser { Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Account is locked out. Try again later.");

        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithIncorrectPassword_ShouldIncrementAccessFailedCount()
    {
        // Arrange
        var email = "user@example.com";
        var password = "WrongPassword";

        var user = new ApplicationUser { Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid credentials.");

        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ValidatePasswordAsync_ShouldNotResetCountOnIncorrectPassword()
    {
        // Arrange
        var email = "user@example.com";
        var password = "WrongPassword";

        var user = new ApplicationUser { Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region GetApplicationUserEmailByUserIdAsync - Happy Path

    [Fact]
    public async Task GetApplicationUserEmailByUserIdAsync_WithValidUserId_ShouldReturnEmail()
    {
        // Arrange
        var userId = "user-123";
        var email = "user@example.com";

        var user = new ApplicationUser { Id = userId, Email = email };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _identityService.GetApplicationUserEmailByUserIdAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().Be(email);
    }

    #endregion

    #region GetApplicationUserEmailByUserIdAsync - Error Conditions

    [Fact]
    public async Task GetApplicationUserEmailByUserIdAsync_WithNonExistentUserId_ShouldReturnFailure()
    {
        // Arrange
        var userId = "nonexistent-id";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _identityService.GetApplicationUserEmailByUserIdAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
        result.Data.Should().BeNull();
    }

    #endregion

    #region UpdateApplicationUserEmailAsync - Happy Path

    [Fact]
    public async Task UpdateApplicationUserEmailAsync_WithValidUserIdAndNewEmail_ShouldUpdateSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var oldEmail = "old@example.com";
        var newEmail = "new@example.com";

        var user = new ApplicationUser { Id = userId, Email = oldEmail };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GenerateChangeEmailTokenAsync(user, newEmail))
            .ReturnsAsync("change-token");

        _userManagerMock
            .Setup(x => x.ChangeEmailAsync(user, newEmail, "change-token"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.SetUserNameAsync(user, newEmail))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.UpdateApplicationUserEmailAsync(userId, newEmail);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _userManagerMock.Verify(x => x.ChangeEmailAsync(user, newEmail, "change-token"), Times.Once);
        _userManagerMock.Verify(x => x.SetUserNameAsync(user, newEmail), Times.Once);
    }

    #endregion

    #region UpdateApplicationUserEmailAsync - Error Conditions

    [Fact]
    public async Task UpdateApplicationUserEmailAsync_WithNonExistentUserId_ShouldReturnFailure()
    {
        // Arrange
        var userId = "nonexistent-id";
        var newEmail = "new@example.com";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _identityService.UpdateApplicationUserEmailAsync(userId, newEmail);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task UpdateApplicationUserEmailAsync_WhenChangeEmailFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var newEmail = "new@example.com";

        var user = new ApplicationUser { Id = userId, Email = "old@example.com" };
        var error = new IdentityError { Description = "Email change failed." };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GenerateChangeEmailTokenAsync(user, newEmail))
            .ReturnsAsync("token");

        _userManagerMock
            .Setup(x => x.ChangeEmailAsync(user, newEmail, "token"))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _identityService.UpdateApplicationUserEmailAsync(userId, newEmail);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Email change failed.");

        _userManagerMock.Verify(x => x.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationUserEmailAsync_WhenSetUserNameFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var newEmail = "new@example.com";

        var user = new ApplicationUser { Id = userId, Email = "old@example.com" };
        var error = new IdentityError { Description = "Username update failed." };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GenerateChangeEmailTokenAsync(user, newEmail))
            .ReturnsAsync("token");

        _userManagerMock
            .Setup(x => x.ChangeEmailAsync(user, newEmail, "token"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.SetUserNameAsync(user, newEmail))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _identityService.UpdateApplicationUserEmailAsync(userId, newEmail);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Username update failed.");
    }

    #endregion

    #region DeleteApplicationUserAsync - Happy Path

    [Fact]
    public async Task DeleteApplicationUserAsync_WithValidUserId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.DeleteApplicationUserAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    #endregion

    #region DeleteApplicationUserAsync - Error Conditions

    [Fact]
    public async Task DeleteApplicationUserAsync_WithNonExistentUserId_ShouldReturnFailure()
    {
        // Arrange
        var userId = "nonexistent-id";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _identityService.DeleteApplicationUserAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");

        _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DeleteApplicationUserAsync_WhenDeletionFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId };

        var error = new IdentityError { Description = "Delete operation failed." };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _identityService.DeleteApplicationUserAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Delete operation failed.");
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData("a@b.c")]
    [InlineData("verylongemailaddresswithlotsofcharacters@verylongdomainname.extension.com")]
    public async Task ValidatePasswordAsync_WithBoundaryEmailValues_ShouldProcessCorrectly(string email)
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser { Id = "id", Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _identityService.ValidatePasswordAsync(email, password);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    [Fact]
    public async Task GetUserAuthStatusAsync_ExistingUser_ReturnsExpectedStatus()
    {
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, UserName = "test@example.com", Email = "test@example.com" };
        var logins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-key-123", "Google")
        };

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        var authStatus = await _identityService.GetUserAuthStatusAsync(userId, CancellationToken.None);

        authStatus.HasPassword.Should().BeTrue();
        authStatus.AuthProviders.Should().Contain("google");
        authStatus.AuthProviders.Should().Contain("password");
        _userManagerMock.Verify(manager => manager.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(manager => manager.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(manager => manager.GetLoginsAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetUserAuthStatusAsync_UserNotFound_ReturnsEmptyProvidersAndNoPassword()
    {
        var userId = "non-existent-user";

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        var authStatus = await _identityService.GetUserAuthStatusAsync(userId, CancellationToken.None);

        authStatus.HasPassword.Should().BeFalse();
        authStatus.AuthProviders.Should().BeEmpty();
        _userManagerMock.Verify(manager => manager.FindByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserAuthStatusAsync_UserWithoutPasswordAndLogins_FallsBackToGoogle()
    {
        var userId = "google-user-789";
        var user = new ApplicationUser { Id = userId, UserName = "fallback@example.com", Email = "fallback@example.com" };

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var authStatus = await _identityService.GetUserAuthStatusAsync(userId, CancellationToken.None);

        authStatus.HasPassword.Should().BeFalse();
        authStatus.AuthProviders.Should().Equal("google");
    }

    [Fact]
    public async Task AddPasswordAsync_WhenUserNotFound_ShouldReturnFail()
    {
        var userId = "non-existent-user";

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        var response = await _identityService.AddPasswordAsync(userId, "NewPassword123!");

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task AddPasswordAsync_WhenUserAlreadyHasPassword_ShouldReturnFail()
    {
        var userId = "existing-user-with-pass";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com" };

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        var response = await _identityService.AddPasswordAsync(userId, "NewPassword123!");

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("User already has a password set. Use change password instead.");
        _userManagerMock.Verify(manager => manager.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddPasswordAsync_WhenAddPasswordFails_ShouldReturnFail()
    {
        var userId = "user-without-pass";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com" };

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(false);

        var identityErrors = new[] { new IdentityError { Description = "Password does not meet complexity." } };
        _userManagerMock
            .Setup(manager => manager.AddPasswordAsync(user, "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var response = await _identityService.AddPasswordAsync(userId, "weak");

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("Password does not meet complexity.");
        _userManagerMock.Verify(manager => manager.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task AddPasswordAsync_WhenSuccessful_ShouldAddPasswordUpdateSecurityStampAndReturnOk()
    {
        var userId = "valid-user";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com" };

        _userManagerMock
            .Setup(manager => manager.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(manager => manager.AddPasswordAsync(user, "ValidPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(manager => manager.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var response = await _identityService.AddPasswordAsync(userId, "ValidPassword123!");

        response.Success.Should().BeTrue();
        response.Errors.Should().BeEmpty();
        _userManagerMock.Verify(manager => manager.AddPasswordAsync(user, "ValidPassword123!"), Times.Once);
        _userManagerMock.Verify(manager => manager.UpdateSecurityStampAsync(user), Times.Once);
    }

    #region InitiatePasswordResetAsync Tests

    [Fact]
    public async Task InitiatePasswordResetAsync_WhenUserDoesNotExist_ShouldReturnGenericSuccessMessageWithNoEmailSent()
    {
        var email = "nonexistent@example.com";
        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _identityService.InitiatePasswordResetAsync(email, "https://app.talentx.com");

        result.Success.Should().BeTrue();
        result.Provider.Should().BeNull();
        result.Message.Should().Be("If an account exists, instructions have been sent.");

        _emailServiceMock.Verify(
            service => service.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_WhenUserHasNoPasswordAndGoogleLogin_ShouldReturnGoogleFailureAndNoEmailSent()
    {
        var email = "google@example.com";
        var user = new ApplicationUser
        {
            Id = "user-google",
            Email = email,
            UserName = "Google User"
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>
            {
                new("Google", "google-subject-id", "Google")
            });

        var result = await _identityService.InitiatePasswordResetAsync(email, "https://app.talentx.com");

        result.Success.Should().BeFalse();
        result.Provider.Should().Be("Google");
        result.Message.Should().Be("This account is registered with Google. Please sign in with Google.");

        _emailServiceMock.Verify(
            service => service.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_WhenUserHasPassword_ShouldGenerateTokenSendEmailAndReturnSuccess()
    {
        var email = "local@example.com";
        var user = new ApplicationUser
        {
            Id = "user-local",
            Email = email,
            UserName = "Local User"
        };
        var generatedToken = "reset-token-12345";
        var frontendBaseUrl = "https://app.talentx.com/";

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        _userManagerMock
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(generatedToken);

        var result = await _identityService.InitiatePasswordResetAsync(email, frontendBaseUrl);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("Local");
        result.Message.Should().Be("If an account exists, instructions have been sent.");

        var expectedResetUrl = $"https://app.talentx.com/reset-password?token={Uri.EscapeDataString(generatedToken)}&email={Uri.EscapeDataString(email)}";

        _emailServiceMock.Verify(
            service => service.SendPasswordResetEmailAsync(
                email,
                "Local User",
                expectedResetUrl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_WhenUserNameIsNull_ShouldUseDefaultUserInEmail()
    {
        var email = "nousername@example.com";
        var user = new ApplicationUser
        {
            Id = "user-local-2",
            Email = email,
            UserName = null
        };
        var generatedToken = "reset-token-xyz";
        var frontendBaseUrl = "http://localhost:3000";

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        _userManagerMock
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(generatedToken);

        var result = await _identityService.InitiatePasswordResetAsync(email, frontendBaseUrl);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("Local");

        var expectedResetUrl = $"http://localhost:3000/reset-password?token={Uri.EscapeDataString(generatedToken)}&email={Uri.EscapeDataString(email)}";

        _emailServiceMock.Verify(
            service => service.SendPasswordResetEmailAsync(
                email,
                "User",
                expectedResetUrl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ResetPasswordWithTokenAsync Tests

    [Fact]
    public async Task ResetPasswordWithTokenAsync_WhenUserDoesNotExist_ShouldReturnFailure()
    {
        var email = "missing@example.com";
        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _identityService.ResetPasswordWithTokenAsync(
            email,
            "any-token",
            "ValidPassword123!");

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid or expired password reset token.");

        _userManagerMock.Verify(
            manager => manager.ResetPasswordAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_WhenResetPasswordFails_ShouldReturnFailure()
    {
        var email = "user@example.com";
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        var invalidToken = "invalid-token";
        var newPassword = "ValidPassword123!";

        _userManagerMock
            .Setup(manager => manager.ResetPasswordAsync(user, invalidToken, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _identityService.ResetPasswordWithTokenAsync(
            email,
            invalidToken,
            newPassword);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid or expired password reset token.");

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_WhenResetPasswordSucceeds_ShouldRevokeAllSessionsAndReturnOk()
    {
        var email = "user@example.com";
        var userId = "user-123";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        var validToken = "valid-token";
        var newPassword = "ValidPassword123!";

        _userManagerMock
            .Setup(manager => manager.ResetPasswordAsync(user, validToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock
            .Setup(service => service.RevokeAllForUserAsync(userId))
            .ReturnsAsync(IdentityResponse.Ok());

        var result = await _identityService.ResetPasswordWithTokenAsync(
            email,
            validToken,
            newPassword);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _userManagerMock.Verify(
            manager => manager.ResetPasswordAsync(user, validToken, newPassword),
            Times.Once);

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_WhenUserIsGuest_ShouldGraduateToTalentRole()
    {
        var email = "guest@example.com";
        var userId = "guest-user-123";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        var validToken = "valid-token";
        var newPassword = "ValidPassword123!";

        _userManagerMock
            .Setup(manager => manager.ResetPasswordAsync(user, validToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(manager => manager.IsInRoleAsync(user, UserRole.Guest.ToString()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.RemoveFromRoleAsync(user, UserRole.Guest.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(manager => manager.IsInRoleAsync(user, UserRole.Talent.ToString()))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(manager => manager.AddToRoleAsync(user, UserRole.Talent.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock
            .Setup(service => service.RevokeAllForUserAsync(userId))
            .ReturnsAsync(IdentityResponse.Ok());

        var result = await _identityService.ResetPasswordWithTokenAsync(
            email,
            validToken,
            newPassword);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _userManagerMock.Verify(
            manager => manager.RemoveFromRoleAsync(user, UserRole.Guest.ToString()),
            Times.Once);

        _userManagerMock.Verify(
            manager => manager.AddToRoleAsync(user, UserRole.Talent.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_WhenUserIsNotGuest_ShouldNotModifyRoles()
    {
        var email = "employer@example.com";
        var userId = "employer-user-123";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        var validToken = "valid-token";
        var newPassword = "ValidPassword123!";

        _userManagerMock
            .Setup(manager => manager.ResetPasswordAsync(user, validToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(manager => manager.IsInRoleAsync(user, UserRole.Guest.ToString()))
            .ReturnsAsync(false);

        _tokenServiceMock
            .Setup(service => service.RevokeAllForUserAsync(userId))
            .ReturnsAsync(IdentityResponse.Ok());

        var result = await _identityService.ResetPasswordWithTokenAsync(
            email,
            validToken,
            newPassword);

        result.Success.Should().BeTrue();

        _userManagerMock.Verify(
            manager => manager.RemoveFromRoleAsync(user, It.IsAny<string>()),
            Times.Never);

        _userManagerMock.Verify(
            manager => manager.AddToRoleAsync(user, It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region ValidatePasswordResetTokenAsync

    [Theory]
    [InlineData("", "token")]
    [InlineData("   ", "token")]
    [InlineData("user@example.com", "")]
    [InlineData("user@example.com", "   ")]
    public async Task ValidatePasswordResetTokenAsync_WhenEmailOrTokenIsInvalid_ShouldReturnFalse(
        string email,
        string token)
    {
        var result = await _identityService.ValidatePasswordResetTokenAsync(email, token);

        result.Should().BeFalse();
        _userManagerMock.Verify(
            manager => manager.FindByEmailAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidatePasswordResetTokenAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        var email = "nonexistent@example.com";
        var token = "some-token";

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _identityService.ValidatePasswordResetTokenAsync(email, token);

        result.Should().BeFalse();
        _userManagerMock.Verify(
            manager => manager.FindByEmailAsync(email),
            Times.Once);
    }

    [Fact]
    public async Task ValidatePasswordResetTokenAsync_WhenTokenIsValid_ShouldReturnTrue()
    {
        var email = "user@example.com";
        var token = "valid-token";
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.VerifyUserTokenAsync(
                user,
                It.IsAny<string>(),
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                token))
            .ReturnsAsync(true);

        var result = await _identityService.ValidatePasswordResetTokenAsync(email, token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordResetTokenAsync_WhenTokenIsExpiredOrInvalid_ShouldReturnFalse()
    {
        var email = "user@example.com";
        var token = "expired-or-used-token";
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = email
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.VerifyUserTokenAsync(
                user,
                It.IsAny<string>(),
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                token))
            .ReturnsAsync(false);

        var result = await _identityService.ValidatePasswordResetTokenAsync(email, token);

        result.Should().BeFalse();
    }

    #endregion
}



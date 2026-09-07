using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Identity.Services;

public class ExternalIdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly ExternalIdentityService _service;

    public ExternalIdentityServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            new Mock<IRoleStore<IdentityRole>>().Object,
            null!, null!, null!, null!);

        _service = new ExternalIdentityService(_userManagerMock.Object, _roleManagerMock.Object);
    }

    [Fact]
    public async Task FindOrCreateExternalUserAsync_ExistingUserWithoutGoogleLogin_AddsLogin()
    {
        var email = "existing@example.com";
        var existingUser = new ApplicationUser { Id = "existing-id", Email = email, UserName = email };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(existingUser))
            .ReturnsAsync(new List<UserLoginInfo>());

        _userManagerMock
            .Setup(manager => manager.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.FindOrCreateExternalUserAsync(email, "Employer");

        result.IsNewUser.Should().BeFalse();
        result.UserId.Should().Be("existing-id");
        _userManagerMock.Verify(
            manager => manager.AddLoginAsync(
                existingUser,
                It.Is<UserLoginInfo>(info => info.LoginProvider == "Google" && info.ProviderKey == email)),
            Times.Once);
    }

    [Fact]
    public async Task FindOrCreateExternalUserAsync_ExistingUserWithGoogleLogin_DoesNotAddLoginAgain()
    {
        var email = "existing@example.com";
        var existingUser = new ApplicationUser { Id = "existing-id", Email = email, UserName = email };
        var existingLogins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-id-123", "Google")
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(existingUser))
            .ReturnsAsync(existingLogins);

        var result = await _service.FindOrCreateExternalUserAsync(email, "Employer");

        result.IsNewUser.Should().BeFalse();
        result.UserId.Should().Be("existing-id");
        _userManagerMock.Verify(
            manager => manager.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()),
            Times.Never);
    }

    [Fact]
    public async Task FindOrCreateExternalUserAsync_NewUser_CreatesUserAddsRoleAndAddsLogin()
    {
        var email = "newuser@example.com";
        var role = "Employer";
        var providerKey = "google-sub-999";

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        _userManagerMock
            .Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock
            .Setup(manager => manager.RoleExistsAsync(role))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), role))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<UserLoginInfo>());

        _userManagerMock
            .Setup(manager => manager.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.FindOrCreateExternalUserAsync(email, role, providerKey);

        result.IsNewUser.Should().BeTrue();
        _userManagerMock.Verify(manager => manager.CreateAsync(It.Is<ApplicationUser>(user => user.Email == email)), Times.Once);
        _userManagerMock.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), role), Times.Once);
        _userManagerMock.Verify(
            manager => manager.AddLoginAsync(
                It.IsAny<ApplicationUser>(),
                It.Is<UserLoginInfo>(info => info.LoginProvider == "Google" && info.ProviderKey == providerKey)),
            Times.Once);
    }
}

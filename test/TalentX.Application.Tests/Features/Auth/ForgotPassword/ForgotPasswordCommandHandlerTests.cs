using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Infrastructure.Identity.Models;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();
        _configurationMock = new Mock<IConfiguration>();

        _handler = new ForgotPasswordCommandHandler(
            _userManagerMock.Object,
            _emailServiceMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnSuccessWithNoEmailSent()
    {
        var email = "nonexistent@example.com";
        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

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
    public async Task Handle_WhenUserIsGoogleOnly_ShouldReturnFailureWithGoogleProviderAndNoEmailSent()
    {
        var email = "googleuser@example.com";
        var user = new ApplicationUser
        {
            Id = "user-google-1",
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

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

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
    public async Task Handle_WhenUserHasPassword_ShouldGenerateTokenSendEmailAndReturnSuccess()
    {
        var email = "localuser@example.com";
        var user = new ApplicationUser
        {
            Id = "user-local-1",
            Email = email,
            UserName = "Local User"
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var generatedToken = "reset-token-12345";
        _userManagerMock
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(generatedToken);

        _configurationMock
            .Setup(config => config["Frontend:Url"])
            .Returns("https://app.talentx.com");

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

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
    public async Task Handle_WhenConfigurationHasNoFrontendUrl_ShouldFallbackToDefaultLocalhostUrl()
    {
        var email = "fallback@example.com";
        var user = new ApplicationUser
        {
            Id = "user-local-2",
            Email = email,
            UserName = null
        };

        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        var generatedToken = "fallback-token";
        _userManagerMock
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(generatedToken);

        _configurationMock
            .Setup(config => config["Frontend:Url"])
            .Returns((string?)null);
        _configurationMock
            .Setup(config => config["App:FrontendUrl"])
            .Returns((string?)null);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

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
}

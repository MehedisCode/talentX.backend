using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Identity.Interfaces;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _configurationMock = new Mock<IConfiguration>();

        _handler = new ForgotPasswordCommandHandler(
            _identityServiceMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task Handle_WithCorsAllowedOriginsContainingStagingUrl_ShouldCallIdentityServiceWithStagingUrl()
    {
        var email = "staging@example.com";
        var stagingUrl = "https://talentx-stg-app.ait-us.co";
        var corsAllowedOrigins = $"http://localhost:3000,{stagingUrl}";
        var expectedResult = new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");

        _configurationMock
            .Setup(configuration => configuration["CORS_ALLOWED_ORIGINS"])
            .Returns(corsAllowedOrigins);

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, stagingUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedResult);
        _identityServiceMock.Verify(
            service => service.InitiatePasswordResetAsync(email, stagingUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCorsFrontendUrlFallback_ShouldCallIdentityServiceWithCorsFrontendUrl()
    {
        var email = "cors@example.com";
        var corsFrontendUrl = "https://staging.talentx.ai";
        var expectedResult = new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");

        _configurationMock
            .Setup(configuration => configuration["CORS_ALLOWED_ORIGINS"])
            .Returns((string?)null);
        _configurationMock
            .Setup(configuration => configuration["Cors:AllowedOrigins"])
            .Returns((string?)null);
        _configurationMock
            .Setup(configuration => configuration["Cors:FrontendUrl"])
            .Returns(corsFrontendUrl);

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, corsFrontendUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedResult);
        _identityServiceMock.Verify(
            service => service.InitiatePasswordResetAsync(email, corsFrontendUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyLocalhostOrigins_ShouldCallIdentityServiceWithLocalhostUrl()
    {
        var email = "dev@example.com";
        var expectedDefaultUrl = "http://localhost:3000";
        var expectedResult = new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");

        _configurationMock
            .Setup(configuration => configuration["CORS_ALLOWED_ORIGINS"])
            .Returns("http://localhost:3000,http://127.0.0.1:3000");

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, expectedDefaultUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedResult);
        _identityServiceMock.Verify(
            service => service.InitiatePasswordResetAsync(email, expectedDefaultUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoCorsConfiguration_ShouldCallIdentityServiceWithLocalhostUrl()
    {
        var email = "local@example.com";
        var expectedDefaultUrl = "http://localhost:3000";
        var expectedResult = new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");

        _configurationMock
            .Setup(configuration => configuration["CORS_ALLOWED_ORIGINS"])
            .Returns((string?)null);
        _configurationMock
            .Setup(configuration => configuration["Cors:AllowedOrigins"])
            .Returns((string?)null);
        _configurationMock
            .Setup(configuration => configuration["Cors:FrontendUrl"])
            .Returns((string?)null);

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, expectedDefaultUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedResult);
        _identityServiceMock.Verify(
            service => service.InitiatePasswordResetAsync(email, expectedDefaultUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsGoogleOnly_ShouldReturnGoogleFailureResultFromIdentityService()
    {
        var email = "google@example.com";
        var expectedResult = new ForgotPasswordResult(false, "Google", "This account is registered with Google. Please sign in with Google.");

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Provider.Should().Be("Google");
        result.Message.Should().Be("This account is registered with Google. Please sign in with Google.");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnSuccessResultFromIdentityService()
    {
        var email = "nonexistent@example.com";
        var expectedResult = new ForgotPasswordResult(true, null, "If an account exists, instructions have been sent.");

        _identityServiceMock
            .Setup(service => service.InitiatePasswordResetAsync(email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new ForgotPasswordCommand(email);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Provider.Should().BeNull();
        result.Message.Should().Be("If an account exists, instructions have been sent.");
    }
}

using FluentAssertions;
using MediatR;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Auth.ResetPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _handler = new ResetPasswordCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordSucceeds_ShouldReturnUnitValue()
    {
        var email = "user@example.com";
        var token = "valid-reset-token";
        var newPassword = "ValidPassword123!";

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _identityServiceMock.Verify(
            service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordFails_ShouldThrowBadRequestException()
    {
        var email = "user@example.com";
        var token = "invalid-or-expired-token";
        var newPassword = "ValidPassword123!";

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Fail("Invalid or expired password reset token."));

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid or expired password reset token.");

        _identityServiceMock.Verify(
            service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Auth.ResetPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Infrastructure.Identity.Models;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();

        _handler = new ResetPasswordCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldThrowBadRequestException()
    {
        var email = "nonexistent@example.com";
        _userManagerMock
            .Setup(manager => manager.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new ResetPasswordCommand(
            email,
            "any-token",
            "ValidPassword123!",
            "ValidPassword123!");

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid or expired password reset token.");

        _userManagerMock.Verify(
            manager => manager.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordFails_ShouldThrowBadRequestException()
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

        var command = new ResetPasswordCommand(
            email,
            invalidToken,
            newPassword,
            newPassword);

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid or expired password reset token.");

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordSucceeds_ShouldRevokeAllUserSessionsAndReturnUnit()
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

        var command = new ResetPasswordCommand(
            email,
            validToken,
            newPassword,
            newPassword);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _userManagerMock.Verify(
            manager => manager.ResetPasswordAsync(user, validToken, newPassword),
            Times.Once);

        _tokenServiceMock.Verify(
            service => service.RevokeAllForUserAsync(userId),
            Times.Once);
    }
}

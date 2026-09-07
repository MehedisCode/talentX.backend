using FluentAssertions;
using MediatR;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Auth.AddPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.AddPassword;

public sealed class AddPasswordCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AddPasswordCommandHandler _handler;

    public AddPasswordCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();

        _handler = new AddPasswordCommandHandler(
            _currentUserServiceMock.Object,
            _identityServiceMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIsUnauthenticated_ShouldThrowUnauthorizedException()
    {
        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var command = new AddPasswordCommand("ValidPassword123!", "ValidPassword123!");

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User is not authenticated.");

        _identityServiceMock.Verify(
            service => service.AddPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasPassword_ShouldThrowBadRequestException()
    {
        var userId = "user-123";
        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(userId);

        _identityServiceMock
            .Setup(service => service.AddPasswordAsync(userId, "ValidPassword123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Fail("User already has a password set. Use change password instead."));

        var command = new AddPasswordCommand("ValidPassword123!", "ValidPassword123!");

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User already has a password set. Use change password instead.");

        _tokenServiceMock.Verify(
            service => service.RevokeOtherSessionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var userId = "nonexistent-user";
        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(userId);

        _identityServiceMock
            .Setup(service => service.AddPasswordAsync(userId, "ValidPassword123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Fail("User not found."));

        var command = new AddPasswordCommand("ValidPassword123!", "ValidPassword123!");

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WhenIdentityOperationFails_ShouldThrowBadRequestException()
    {
        var userId = "user-123";
        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(userId);

        _identityServiceMock
            .Setup(service => service.AddPasswordAsync(userId, "ValidPassword123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Fail(new[] { "Password does not meet complexity requirements." }));

        var command = new AddPasswordCommand("ValidPassword123!", "ValidPassword123!");

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Password does not meet complexity requirements.");
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldAddPasswordRevokeOtherSessionsAndReturnUnit()
    {
        var userId = "user-123";
        var currentRefreshToken = "refresh-token-abc";
        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(userId);

        _identityServiceMock
            .Setup(service => service.AddPasswordAsync(userId, "ValidPassword123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        _tokenServiceMock
            .Setup(service => service.RevokeOtherSessionsAsync(userId, currentRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        var command = new AddPasswordCommand("ValidPassword123!", "ValidPassword123!", currentRefreshToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _identityServiceMock.Verify(
            service => service.AddPasswordAsync(userId, "ValidPassword123!", It.IsAny<CancellationToken>()),
            Times.Once);

        _tokenServiceMock.Verify(
            service => service.RevokeOtherSessionsAsync(userId, currentRefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

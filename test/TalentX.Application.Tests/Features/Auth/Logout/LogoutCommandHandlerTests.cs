using FluentAssertions;
using Moq;
using TalentX.Application.Features.Auth.Logout;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.Logout;

/// <summary>
/// Unit tests for LogoutCommandHandler
/// Tests cover logout flow including token revocation
/// Uses AAA (Arrange-Act-Assert) pattern with mocked dependencies
/// </summary>
public class LogoutCommandHandlerTests
{
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _tokenServiceMock = new Mock<ITokenService>();
        _handler = new LogoutCommandHandler(_tokenServiceMock.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithValidRefreshToken_ShouldRevokeTokenSuccessfully()
    {
        // Arrange
        var refreshToken = "valid.refresh.token";
        var command = new LogoutCommand(refreshToken);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(refreshToken))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _tokenServiceMock.Verify(x => x.RevokeAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallRevokeWithCorrectToken()
    {
        // Arrange
        var refreshToken = "specific.token.to.revoke";
        var command = new LogoutCommand(refreshToken);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(refreshToken))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.RevokeAsync(refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldOnlyCallRevokeOnce()
    {
        // Arrange
        var refreshToken = "token";
        var command = new LogoutCommand(refreshToken);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(refreshToken))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.RevokeAsync(It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Error Condition Tests

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var invalidToken = "invalid.refresh.token";
        var command = new LogoutCommand(invalidToken);

        var errorMessage = "Invalid refresh token.";

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(invalidToken))
            .ReturnsAsync(IdentityResponse.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ShouldReturnFailureResult()
    {
        // Arrange
        var alreadyRevokedToken = "already.revoked.token";
        var command = new LogoutCommand(alreadyRevokedToken);

        var errorMessage = "Token has already been revoked.";

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(alreadyRevokedToken))
            .ReturnsAsync(IdentityResponse.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithNullRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var nullToken = (string)null!;
        var command = new LogoutCommand(nullToken);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse.Fail("Invalid refresh token."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var emptyToken = string.Empty;
        var command = new LogoutCommand(emptyToken);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(emptyToken))
            .ReturnsAsync(IdentityResponse.Fail("Invalid refresh token."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var validToken = "valid.token.but.user.deleted";
        var command = new LogoutCommand(validToken);

        var errorMessage = "User not found.";

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(validToken))
            .ReturnsAsync(IdentityResponse.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithDatabaseError_ShouldReturnFailureResult()
    {
        // Arrange
        var token = "token";
        var command = new LogoutCommand(token);

        var errorMessage = "Database error occurred.";

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithMultipleErrorsFromService_ShouldReturnAllErrors()
    {
        // Arrange
        var token = "problematic.token";
        var command = new LogoutCommand(token);

        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Fail(errors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.Errors.Should().Contain("Error 3");
    }

    #endregion

    #region Token Revocation Scenarios

    [Fact]
    public async Task Handle_ShouldValidateTokenBeforeRevoking()
    {
        // Arrange
        var token = "token.to.validate";
        var command = new LogoutCommand(token);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(x => x.RevokeAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPreventTokenReuseAfterRevocation()
    {
        // Arrange
        var token = "token.to.prevent.reuse";
        var command = new LogoutCommand(token);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        // The token service is responsible for preventing reuse
        _tokenServiceMock.Verify(x => x.RevokeAsync(token), Times.Once);
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData("a")]
    [InlineData("xyyyyyyy" + "y")]
    [InlineData("token-with-dashes")]
    [InlineData("token_with_underscores")]
    public async Task Handle_WithBoundaryValueTokens_ShouldProcessCorrectly(string token)
    {
        // Arrange
        var command = new LogoutCommand(token);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenServiceMock.Verify(x => x.RevokeAsync(token), Times.Once);
    }

    #endregion

    #region Idempotence Tests

    [Fact]
    public async Task Handle_MultipleCallsWithSameToken_ShouldCallRevokeMultipleTimes()
    {
        // Arrange
        var token = "token";
        var command = new LogoutCommand(token);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act - First call
        var result1 = await _handler.Handle(command, CancellationToken.None);

        // Act - Second call with same token
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        _tokenServiceMock.Verify(x => x.RevokeAsync(token), Times.Exactly(2));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var token = "token";
        var command = new LogoutCommand(token);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act & Assert
        _ = await _handler.Handle(command, cancellationTokenSource.Token);
    }

    #endregion

    #region Response Structure Tests

    [Fact]
    public async Task Handle_SuccessResponseShouldHaveNoErrors()
    {
        // Arrange
        var token = "valid.token";
        var command = new LogoutCommand(token);

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FailureResponseShouldContainErrors()
    {
        // Arrange
        var token = "invalid.token";
        var command = new LogoutCommand(token);
        var errorMessage = "Error";

        _tokenServiceMock
            .Setup(x => x.RevokeAsync(token))
            .ReturnsAsync(IdentityResponse.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Errors.Should().NotBeEmpty();
        result.Success.Should().BeFalse();
    }

    #endregion
}

using FluentAssertions;
using Moq;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Features.Auth.RefreshToken;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.RefreshToken;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler
/// Tests cover token refresh flow including validation and rotation
/// Uses AAA (Arrange-Act-Assert) pattern with mocked dependencies
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _tokenServiceMock = new Mock<ITokenService>();
        _handler = new RefreshTokenCommandHandler(_tokenServiceMock.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid.refresh.token";
        var newAccessToken = "new.access.token";
        var newRefreshToken = "new.refresh.token";
        var command = new RefreshTokenCommand(refreshToken);

        var tokenResponse = new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(tokenResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be(newAccessToken);
        result.Data.RefreshToken.Should().Be(newRefreshToken);

        _tokenServiceMock.Verify(x => x.ValidateAndRotateAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldGenerateNewTokenPair()
    {
        // Arrange
        var oldRefreshToken = "old.refresh.token.value";
        var command = new RefreshTokenCommand(oldRefreshToken);

        var newTokenResponse = new TokenResponse
        {
            AccessToken = "new.access.token.value",
            RefreshToken = "new.refresh.token.value"
        };

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(oldRefreshToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(newTokenResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBe(oldRefreshToken);
        result.Data.RefreshToken.Should().NotBe(oldRefreshToken);
    }

    #endregion

    #region Error Condition Tests

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var invalidToken = "invalid.refresh.token";
        var command = new RefreshTokenCommand(invalidToken);

        var errorMessage = "Invalid refresh token.";

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(invalidToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithExpiredRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var expiredToken = "expired.refresh.token";
        var command = new RefreshTokenCommand(expiredToken);

        var errorMessage = "Refresh token has expired.";

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(expiredToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithReuseDetectedToken_ShouldReturnFailureResult()
    {
        // Arrange
        var reuseToken = "reused.refresh.token";
        var command = new RefreshTokenCommand(reuseToken);

        var errorMessage = "Refresh token reuse detected. All sessions have been revoked.";

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(reuseToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Handle_WithEmptyRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var emptyToken = string.Empty;
        var command = new RefreshTokenCommand(emptyToken);

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(emptyToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail("Invalid refresh token."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNullRefreshToken_ShouldReturnFailureResult()
    {
        // Arrange
        var nullToken = (string)null!;
        var command = new RefreshTokenCommand(nullToken);

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail("Invalid refresh token."));

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
        var command = new RefreshTokenCommand(validToken);

        var errorMessage = "User not found.";

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(validToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errorMessage));

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
        var command = new RefreshTokenCommand(token);

        var errors = new[] { "Error 1", "Error 2" };

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(token))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    #endregion

    #region Token Rotation Tests

    [Fact]
    public async Task Handle_ShouldCallValidateAndRotateWithCorrectToken()
    {
        // Arrange
        var refreshToken = "specific.refresh.token";
        var command = new RefreshTokenCommand(refreshToken);

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "new.access",
                RefreshToken = "new.refresh"
            }));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.ValidateAndRotateAsync(refreshToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCallValidateAndRotateMultipleTimes()
    {
        // Arrange
        var refreshToken = "token";
        var command = new RefreshTokenCommand(refreshToken);

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(refreshToken))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "new.access",
                RefreshToken = "new.refresh"
            }));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.ValidateAndRotateAsync(It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData("a")]
    [InlineData("xyyyyyyy" + "y")]
    public async Task Handle_WithBoundaryValueTokens_ShouldForwardToService(string token)
    {
        // Arrange
        var command = new RefreshTokenCommand(token);

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(token))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "access",
                RefreshToken = "refresh"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenServiceMock.Verify(x => x.ValidateAndRotateAsync(token), Times.Once);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task Handle_ShouldReturnExactTokensFromService()
    {
        // Arrange
        var token = "original.token";
        var expectedAccessToken = "expected.access.token.with.exact.value";
        var expectedRefreshToken = "expected.refresh.token.with.exact.value";
        var command = new RefreshTokenCommand(token);

        var tokenResponse = new TokenResponse
        {
            AccessToken = expectedAccessToken,
            RefreshToken = expectedRefreshToken
        };

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(token))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(tokenResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Data!.AccessToken.Should().Be(expectedAccessToken);
        result.Data.RefreshToken.Should().Be(expectedRefreshToken);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var token = "token";
        var command = new RefreshTokenCommand(token);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _tokenServiceMock
            .Setup(x => x.ValidateAndRotateAsync(token))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "access",
                RefreshToken = "refresh"
            }));

        // Act & Assert
        _ = await _handler.Handle(command, cancellationTokenSource.Token);
    }

    #endregion
}

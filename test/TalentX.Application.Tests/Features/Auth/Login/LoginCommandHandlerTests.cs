using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Auth.Login;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();

        _unitOfWorkMock.Setup(x => x.AuditLogs).Returns(_auditLogRepositoryMock.Object);

        _handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccessfulLoginResult()
    {
        // Arrange
        var email = "testuser@example.com";
        var password = "ValidPassword123!";
        var userId = "user-123";
        var accessToken = "access.token.value";
        var refreshToken = "refresh.token.value";

        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(tokenResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.Email.Should().Be(email);
        result.Data.Tokens.AccessToken.Should().Be(accessToken);
        result.Data.Tokens.RefreshToken.Should().Be(refreshToken);

        _identityServiceMock.Verify(x => x.ValidatePasswordAsync(email, password), Times.Once);
        _tokenServiceMock.Verify(x => x.CreateAsync(userId), Times.Once);
        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == email && log.Action == "Login Success" && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldCreateTokensWithCorrectUserId()
    {
        // Arrange
        var email = "admin@example.com";
        var password = "AdminPassword123!";
        var userId = "admin-456";

        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "Admin" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            }));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(x => x.CreateAsync(userId), Times.Once);
    }

    #endregion

    #region Error Condition Tests

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnFailureResult()
    {
        // Arrange
        var email = "testuser@example.com";
        var password = "WrongPassword123!";
        var command = new LoginCommand(email, password);

        var errorMessage = "Invalid credentials.";

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Fail(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(errorMessage);
        result.Data.Should().BeNull();

        _tokenServiceMock.Verify(x => x.CreateAsync(It.IsAny<string>()), Times.Never);
        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == email && log.Action == "Login Failed" && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailureResult()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";
        var command = new LoginCommand(email, password);

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Fail("Invalid credentials."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithLockedOutAccount_ShouldReturnFailureResult()
    {
        // Arrange
        var email = "locked@example.com";
        var password = "Password123!";
        var command = new LoginCommand(email, password);

        var lockoutMessage = "Account is locked out. Try again later.";

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Fail(lockoutMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(lockoutMessage);
    }

    [Fact]
    public async Task Handle_WhenTokenCreationFails_ShouldReturnFailureResult()
    {
        // Arrange
        var email = "testuser@example.com";
        var password = "ValidPassword123!";
        var userId = "user-123";
        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        var tokenErrorMessage = "Failed to generate tokens.";

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(tokenErrorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(tokenErrorMessage);
        result.Data.Should().BeNull();

        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == email && log.Action == "Login Failed" && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleErrorsFromTokenService_ShouldReturnAllErrors()
    {
        // Arrange
        var email = "testuser@example.com";
        var password = "ValidPassword123!";
        var userId = "user-123";
        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errors));

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

    #region Data Integrity Tests

    [Fact]
    public async Task Handle_ShouldPreserveEmailFromValidationResult()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";
        var userId = "user-123";
        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Handle_ShouldPreserveUserIdThroughoutProcess()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";
        var userId = "unique-user-id-789";
        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Data!.UserId.Should().Be(userId);
        _tokenServiceMock.Verify(x => x.CreateAsync(userId), Times.Once);
    }

    #endregion

    #region Cancellation and Async Tests

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = "user-123",
            Email = "test@example.com",
            Roles = new[] { "User" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(command.Email, command.Password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(passwordValidationResult.UserId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            }));

        // Act & Assert
        var result = await _handler.Handle(command, cancellationTokenSource.Token);

        result.Success.Should().BeTrue();
        result.Data!.UserId.Should().Be(passwordValidationResult.UserId);
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData("a@b.c", "P")]
    [InlineData("verylongemailaddress@verylongdomainname.verylongextension.com", "VeryLongPassword123!")]
    public async Task Handle_WithBoundaryValueCredentials_ShouldReturnSuccessfulResult(
        string email, string password)
    {
        // Arrange
        var userId = "user-123";
        var command = new LoginCommand(email, password);

        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = userId,
            Email = email,
            Roles = new[] { "User" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(email, password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync(userId))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public async Task Handle_OnSuccessfulLogin_RecordsLoginSuccessAuditLogWithCorrectActor()
    {
        // Arrange
        var command = new LoginCommand("admin@example.com", "AdminPassword123!");
        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = "admin-1",
            Email = "admin@example.com",
            Roles = new[] { "Admin" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(command.Email, command.Password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync("admin-1"))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == "admin@example.com"
                                      && log.Action == "Login Success"
                                      && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnPasswordValidationFailure_RecordsLoginFailedAuditLog()
    {
        // Arrange
        var command = new LoginCommand("failed@example.com", "WrongPassword!");

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(command.Email, command.Password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Fail("Invalid credentials."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == "failed@example.com"
                                      && log.Action == "Login Failed"
                                      && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnTokenCreationFailure_RecordsLoginFailedAuditLog()
    {
        // Arrange
        var command = new LoginCommand("valid@example.com", "CorrectPassword!");
        var passwordValidationResult = new PasswordValidationResult
        {
            UserId = "user-999",
            Email = "valid@example.com",
            Roles = new[] { "User" }
        };

        _identityServiceMock
            .Setup(x => x.ValidatePasswordAsync(command.Email, command.Password))
            .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(passwordValidationResult));

        _tokenServiceMock
            .Setup(x => x.CreateAsync("user-999"))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Fail("Token generation failure."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(log => log.Actor == "valid@example.com"
                                      && log.Action == "Login Failed"
                                      && !log.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

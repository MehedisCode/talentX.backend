using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TalentX.Application.Features.Users.Constants;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Identity.Services;
using TalentX.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TalentX.Infrastructure.Tests.Identity.Services;

/// <summary>
/// Unit tests for TokenService
/// Tests cover JWT access token generation, refresh token creation, rotation, and revocation
/// Uses AAA (Arrange-Act-Assert) pattern with mocked dependencies and in-memory database
/// </summary>
public class TokenServiceTests : IAsyncLifetime
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsOptionsMock;
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Create JWT settings for testing
        _jwtSettings = new JwtSettings
        {
            Secret = "this-is-a-very-long-secret-key-for-testing-purposes-at-least-32-characters",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        _jwtSettingsOptionsMock = new Mock<IOptions<JwtSettings>>();
        _jwtSettingsOptionsMock.Setup(x => x.Value).Returns(_jwtSettings);

        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TokenServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _tokenService = new TokenService(_userManagerMock.Object, _context, _jwtSettingsOptionsMock.Object);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    #region CreateAsync - Happy Path Tests

    [Fact]
    public async Task CreateAsync_WithValidUserId_ShouldCreateAccessAndRefreshTokens()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify tokens are different
        result.Data.AccessToken.Should().NotBe(result.Data.RefreshToken);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateValidJwtAccessToken()
    {
        // Arrange
        var userId = "user-123";
        var email = "user@example.com";
        var username = "testuser";
        var user = new ApplicationUser { Id = userId, Email = email, UserName = username };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Data.Should().NotBeNull();

        // Decode JWT to verify claims
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public async Task CreateAsync_ShouldIncludeAllUserRolesInAccessToken()
    {
        // Arrange
        var userId = "user-123";
        var roles = new List<string> { "User", "Admin", "Manager" };
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(3);
        roleClaims.Should().Contain(c => c.Value == "User");
        roleClaims.Should().Contain(c => c.Value == "Admin");
        roleClaims.Should().Contain(c => c.Value == "Manager");
    }

    [Fact]
    public async Task CreateAsync_ShouldStoreRefreshTokenHashInDatabase()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        var storedTokens = await _context.RefreshTokens.ToListAsync();
        storedTokens.Should().HaveCount(1);

        var storedToken = storedTokens.First();
        storedToken.ApplicationUserId.Should().Be(userId);
        storedToken.TokenHash.Should().NotBeNullOrEmpty();
        storedToken.RevokedAt.Should().BeNull();
        storedToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCorrectRefreshTokenExpiration()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };
        var beforeCall = DateTime.UtcNow;

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        await _tokenService.CreateAsync(userId);
        var afterCall = DateTime.UtcNow;

        // Assert
        var storedToken = await _context.RefreshTokens.FirstAsync();
        var expectedExpiration = beforeCall.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        var actualExpiration = storedToken.ExpiresAt;

        actualExpiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_WithMultipleUsers_ShouldCreateSeparateTokens()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user-1", Email = "user1@example.com", UserName = "user1" };
        var user2 = new ApplicationUser { Id = "user-2", Email = "user2@example.com", UserName = "user2" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync("user-1"))
            .ReturnsAsync(user1);

        _userManagerMock
            .Setup(x => x.FindByIdAsync("user-2"))
            .ReturnsAsync(user2);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result1 = await _tokenService.CreateAsync("user-1");
        var result2 = await _tokenService.CreateAsync("user-2");

        // Assert
        result1.Data!.RefreshToken.Should().NotBe(result2.Data!.RefreshToken);

        var storedTokens = await _context.RefreshTokens.ToListAsync();
        storedTokens.Should().HaveCount(2);
        storedTokens.Should().Contain(t => t.ApplicationUserId == "user-1");
        storedTokens.Should().Contain(t => t.ApplicationUserId == "user-2");
    }

    #endregion

    #region CreateAsync - Error Conditions

    [Fact]
    public async Task CreateAsync_WithNonExistentUserId_ShouldReturnFailure()
    {
        // Arrange
        var userId = "nonexistent-user";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
        result.Data.Should().BeNull();
    }

    #endregion

    #region CreateAsync - Deactivated Account

    [Fact]
    public async Task CreateAsync_WithDeactivatedDomainUser_ShouldReturnFailureAndNotIssueTokens()
    {
        // Arrange
        var userId = "user-123";
        var identityUser = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };
        var domainUser = new User(userId, "Jordan Smith", "user@example.com", UserRole.Talent, isVerified: true);
        domainUser.Deactivate();
        _context.DomainUsers.Add(domainUser);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(AccountStatusMessages.Deactivated);
        result.Data.Should().BeNull();

        var storedTokens = await _context.RefreshTokens.ToListAsync();
        storedTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithActiveDomainUser_ShouldStillIssueTokens()
    {
        // Arrange
        var userId = "user-123";
        var identityUser = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };
        var domainUser = new User(userId, "Jordan Smith", "user@example.com", UserRole.Talent, isVerified: true);
        _context.DomainUsers.Add(domainUser);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(identityUser);
        _userManagerMock
            .Setup(x => x.GetRolesAsync(identityUser))
            .ReturnsAsync(new List<string> { "Talent" });

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    #endregion

    #region ValidateAndRotateAsync - Happy Path Tests

    [Fact]
    public async Task ValidateAndRotateAsync_WithValidToken_ShouldReturnNewTokenPair()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        // Create initial token
        var createResult = await CreateInitialToken(userId, user);
        var oldRefreshToken = createResult.RefreshToken;

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(oldRefreshToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        // New refresh token should be different from old one
        result.Data.RefreshToken.Should().NotBe(oldRefreshToken);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ShouldRevokeOldTokenAndCreateNew()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var oldRefreshToken = createResult.RefreshToken;

        var beforeRotation = DateTime.UtcNow;

        // Act
        await _tokenService.ValidateAndRotateAsync(oldRefreshToken);

        var afterRotation = DateTime.UtcNow;

        // Assert
        var storedTokens = await _context.RefreshTokens.ToListAsync();
        storedTokens.Should().HaveCount(2);

        var oldToken = storedTokens.First();
        var newToken = storedTokens.Last();

        oldToken.IsActive.Should().BeFalse(); // Should be revoked
        oldToken.RevokedAt.Should().NotBeNull();
        oldToken.RevokedAt.Should().BeAfter(beforeRotation).And.BeBefore(afterRotation);

        newToken.IsActive.Should().BeTrue(); // Should be active
        newToken.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ShouldGenerateValidNewAccessToken()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var oldRefreshToken = createResult.RefreshToken;

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(oldRefreshToken);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
    }

    #endregion

    #region ValidateAndRotateAsync - Deactivated Account

    [Fact]
    public async Task ValidateAndRotateAsync_WithDeactivatedDomainUser_ShouldReturnFailureAndRevokeToken()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        var domainUser = new User(userId, "Jordan Smith", "user@example.com", UserRole.Talent, isVerified: true);
        domainUser.Deactivate();
        _context.DomainUsers.Add(domainUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(AccountStatusMessages.Deactivated);
        result.Data.Should().BeNull();

        var storedToken = await _context.RefreshTokens.FirstAsync(t => t.ApplicationUserId == userId);
        storedToken.IsActive.Should().BeFalse();
        storedToken.RevokedAt.Should().NotBeNull();

        var allTokens = await _context.RefreshTokens.Where(t => t.ApplicationUserId == userId).ToListAsync();
        allTokens.Should().HaveCount(1); // no new token was issued
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithActiveDomainUser_ShouldStillRotate()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        var domainUser = new User(userId, "Jordan Smith", "user@example.com", UserRole.Talent, isVerified: true);
        _context.DomainUsers.Add(domainUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(refreshToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefreshToken.Should().NotBe(refreshToken);
    }

    #endregion

    #region ValidateAndRotateAsync - Error Conditions

    [Fact]
    public async Task ValidateAndRotateAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidToken = "invalid.token.that.doesnt.exist";

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(invalidToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid refresh token.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithReuseDetection_ShouldRevokeAllUserSessions()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        // Create initial token
        var createResult = await CreateInitialToken(userId, user);
        var oldRefreshToken = createResult.RefreshToken;

        // First rotation - should succeed
        var firstRotation = await _tokenService.ValidateAndRotateAsync(oldRefreshToken);
        var newToken = firstRotation.Data!.RefreshToken;

        // Attempt to use old token again (reuse detection)
        // Act
        var result = await _tokenService.ValidateAndRotateAsync(oldRefreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Refresh token reuse detected. All sessions have been revoked.");

        // Verify all tokens are revoked
        var allTokens = await _context.RefreshTokens.ToListAsync();
        allTokens.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithAlreadyRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        // Manually revoke the token
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _context.RefreshTokens.FirstAsync(t => t.TokenHash == tokenHash);
        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Refresh token reuse detected. All sessions have been revoked.");
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WhenUserDeleted_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        // Setup mock to return null for deleted user
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nullToken = (string)null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _tokenService.ValidateAndRotateAsync(nullToken));
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithEmptyToken_ShouldReturnFailure()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(emptyToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid refresh token.");
    }

    #endregion

    #region RevokeAsync - Happy Path Tests

    [Fact]
    public async Task RevokeAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        var beforeRevoke = DateTime.UtcNow;

        // Act
        var result = await _tokenService.RevokeAsync(refreshToken);

        var afterRevoke = DateTime.UtcNow;

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        var storedToken = await _context.RefreshTokens.FirstAsync();
        storedToken.IsActive.Should().BeFalse();
        storedToken.RevokedAt.Should().NotBeNull();
        storedToken.RevokedAt.Should().BeAfter(beforeRevoke).And.BeBefore(afterRevoke);
    }

    [Fact]
    public async Task RevokeAsync_ShouldNotAffectOtherUserTokens()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user-1", Email = "user1@example.com", UserName = "user1" };
        var user2 = new ApplicationUser { Id = "user-2", Email = "user2@example.com", UserName = "user2" };

        var token1 = await CreateInitialToken("user-1", user1);
        var token2 = await CreateInitialToken("user-2", user2);

        // Act
        await _tokenService.RevokeAsync(token1.RefreshToken);

        // Assert
        var storedTokens = await _context.RefreshTokens.ToListAsync();
        var revokedToken = storedTokens.First(t => t.ApplicationUserId == "user-1");
        var otherToken = storedTokens.First(t => t.ApplicationUserId == "user-2");

        revokedToken.IsActive.Should().BeFalse();
        otherToken.IsActive.Should().BeTrue();
    }

    #endregion

    #region RevokeAsync - Error Conditions

    [Fact]
    public async Task RevokeAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidToken = "invalid.token";

        // Act
        var result = await _tokenService.RevokeAsync(invalidToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid refresh token.");
    }

    [Fact]
    public async Task RevokeAsync_WithAlreadyRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var refreshToken = createResult.RefreshToken;

        // Revoke the token
        await _tokenService.RevokeAsync(refreshToken);

        // Act - Try to revoke again
        var result = await _tokenService.RevokeAsync(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid refresh token.");
    }

    [Fact]
    public async Task RevokeAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nullToken = (string)null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _tokenService.RevokeAsync(nullToken));
    }

    #endregion

    #region RevokeAllForUserAsync - Happy Path Tests

    [Fact]
    public async Task RevokeAllForUserAsync_WithValidUserId_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        // Create multiple tokens for same user
        await CreateInitialToken(userId, user);
        await CreateInitialToken(userId, user);
        await CreateInitialToken(userId, user);

        // Act
        var result = await _tokenService.RevokeAllForUserAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        var userTokens = await _context.RefreshTokens
            .Where(t => t.ApplicationUserId == userId)
            .ToListAsync();

        userTokens.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
        userTokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task RevokeAllForUserAsync_ShouldNotAffectOtherUsersTokens()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user-1", Email = "user1@example.com", UserName = "user1" };
        var user2 = new ApplicationUser { Id = "user-2", Email = "user2@example.com", UserName = "user2" };

        await CreateInitialToken("user-1", user1);
        await CreateInitialToken("user-2", user2);

        // Act
        await _tokenService.RevokeAllForUserAsync("user-1");

        // Assert
        var user1Tokens = await _context.RefreshTokens
            .Where(t => t.ApplicationUserId == "user-1")
            .ToListAsync();

        var user2Tokens = await _context.RefreshTokens
            .Where(t => t.ApplicationUserId == "user-2")
            .ToListAsync();

        user1Tokens.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
        user2Tokens.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task RevokeAllForUserAsync_WithUserWithoutTokens_ShouldReturnSuccess()
    {
        // Arrange
        var userId = "user-with-no-tokens";

        // Act
        var result = await _tokenService.RevokeAllForUserAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_ShouldOnlyRevokeActiveTokens()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        // Create tokens and revoke some
        var token1 = await CreateInitialToken(userId, user);
        var token2 = await CreateInitialToken(userId, user);
        var token3 = await CreateInitialToken(userId, user);

        await _tokenService.RevokeAsync(token1.RefreshToken);

        var beforeRevoke = DateTime.UtcNow;

        // Act
        await _tokenService.RevokeAllForUserAsync(userId);

        var afterRevoke = DateTime.UtcNow;

        // Assert
        var allTokens = await _context.RefreshTokens
            .Where(t => t.ApplicationUserId == userId)
            .ToListAsync();

        allTokens.Should().HaveCount(3);
        allTokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
        
        // Previously revoked token's RevokedAt should remain unchanged
        var firstToken = allTokens.First();
        firstToken.RevokedAt.Should().BeBefore(beforeRevoke);
    }

    #endregion

    #region RevokeOtherSessionsAsync Tests

    [Fact]
    public async Task RevokeOtherSessionsAsync_RevokesAllExceptCurrentToken()
    {
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var firstTokenResponse = await CreateInitialToken(userId, user);
        var currentTokenResponse = await CreateInitialToken(userId, user);
        var thirdTokenResponse = await CreateInitialToken(userId, user);

        var result = await _tokenService.RevokeOtherSessionsAsync(userId, currentTokenResponse.RefreshToken);

        result.Success.Should().BeTrue();

        var tokens = await _context.RefreshTokens
            .Where(token => token.ApplicationUserId == userId)
            .ToListAsync();

        var currentHash = HashToken(currentTokenResponse.RefreshToken);
        var activeTokens = tokens.Where(token => token.RevokedAt == null).ToList();
        var revokedTokens = tokens.Where(token => token.RevokedAt != null).ToList();

        activeTokens.Should().ContainSingle();
        activeTokens.Single().TokenHash.Should().Be(currentHash);
        revokedTokens.Should().HaveCount(2);
    }

    [Fact]
    public async Task RevokeOtherSessionsAsync_WhenCurrentRefreshTokenIsNullOrEmpty_ShouldRevokeAllActiveTokens()
    {
        var userId = "user-456";
        var user = new ApplicationUser { Id = userId, Email = "user456@example.com", UserName = "testuser456" };

        await CreateInitialToken(userId, user);
        await CreateInitialToken(userId, user);

        var result = await _tokenService.RevokeOtherSessionsAsync(userId, null);

        result.Success.Should().BeTrue();

        var tokens = await _context.RefreshTokens
            .Where(token => token.ApplicationUserId == userId)
            .ToListAsync();

        tokens.Should().AllSatisfy(token => token.RevokedAt.Should().NotBeNull());
    }

    #endregion


    #region Token Hash and Validation Tests

    [Fact]
    public async Task TokenCreation_ShouldHashRefreshTokenBeforeSaving()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _tokenService.CreateAsync(userId);
        var rawToken = result.Data!.RefreshToken;

        // Assert
        var storedToken = await _context.RefreshTokens.FirstAsync();
        
        // The hash should not equal the raw token
        storedToken.TokenHash.Should().NotBe(rawToken);
        
        // The hash should be consistent (same raw token should produce same hash when hashed the same way)
        var computedHash = HashToken(rawToken);
        storedToken.TokenHash.Should().Be(computedHash);
    }

    [Fact]
    public async Task ValidateAndRotate_RefreshTokenFormats_ShouldBeDifferent()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        var createResult = await CreateInitialToken(userId, user);
        var oldToken = createResult.RefreshToken;

        // Act
        var rotateResult = await _tokenService.ValidateAndRotateAsync(oldToken);
        var newToken = rotateResult.Data!.RefreshToken;

        // Assert
        oldToken.Should().NotBe(newToken);
        oldToken.Length.Should().BeGreaterThan(0);
        newToken.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public async Task CreateAsync_WithUserHavingNoRoles_ShouldCreateTokenWithoutRoleClaims()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>()); // No roles

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithUserHavingMaxRoles_ShouldIncludeAllRoles()
    {
        // Arrange
        var userId = "user-123";
        var roles = Enumerable.Range(0, 10).Select(i => $"Role{i}").ToList();
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", UserName = "testuser" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeTrue();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(10);
    }

    [Fact]
    public async Task CreateAsync_WhenDomainUserExists_IncludesDomainUserNameInClaimTypesName()
    {
        // Arrange
        var userId = "user-employer-123";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "fariha.jabin@gmail.com",
            Email = "fariha.jabin@gmail.com"
        };

        var employer = new Employer(userId, "Fariha Jabin", "fariha.jabin@gmail.com", "Acme Corp");
        await _context.DomainUsers.AddAsync(employer);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Employer" });

        // Act
        var result = await _tokenService.CreateAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);
        var nameClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("Fariha Jabin");
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WhenDomainUserExists_IncludesDomainUserNameInClaimTypesName()
    {
        // Arrange
        var userId = "user-employer-rotate";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "fariha.jabin@gmail.com",
            Email = "fariha.jabin@gmail.com"
        };

        var employer = new Employer(userId, "Fariha Jabin", "fariha.jabin@gmail.com", "Acme Corp");
        await _context.DomainUsers.AddAsync(employer);
        await _context.SaveChangesAsync();

        var initialToken = await CreateInitialToken(userId, user);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Employer" });

        // Act
        var result = await _tokenService.ValidateAndRotateAsync(initialToken.RefreshToken);

        // Assert
        result.Success.Should().BeTrue();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);
        var nameClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("Fariha Jabin");
    }

    #endregion

    #region Helper Methods

    private async Task<TokenResponse> CreateInitialToken(string userId, ApplicationUser user)
    {
        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _tokenService.CreateAsync(userId);
        return result.Data!;
    }

    private string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashedBytes);
    }

    #endregion
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TalentX.Application.Features.Users.Constants;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Identity.Services;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings
    )
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<IdentityResponse<TokenResponse>> CreateAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse<TokenResponse>.Fail("User not found.");
        }

        var domainUser = await _context.DomainUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (domainUser is not null && domainUser.Status == AccountStatus.Deactivated)
        {
            return IdentityResponse<TokenResponse>.Fail(AccountStatusMessages.Deactivated);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = GenerateAccessToken(user, roles, domainUser?.Name);
        var rawRefreshToken = GenerateRawToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            ApplicationUserId = user.Id,
            TokenHash = Hash(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
        await _context.SaveChangesAsync();

        return IdentityResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken
        });
    }

    public async Task<IdentityResponse<TokenResponse>> ValidateAndRotateAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null)
            return IdentityResponse<TokenResponse>.Fail("Invalid refresh token.");

        if (!stored.IsActive)
        {
            await RevokeAllForUserAsync(stored.ApplicationUserId);
            return IdentityResponse<TokenResponse>.Fail("Refresh token reuse detected. All sessions have been revoked.");
        }

        var user = await _userManager.FindByIdAsync(stored.ApplicationUserId);
        if (user is null)
            return IdentityResponse<TokenResponse>.Fail("User not found.");

        var domainUser = await _context.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == stored.ApplicationUserId);
        if (domainUser is not null && domainUser.Status == AccountStatus.Deactivated)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return IdentityResponse<TokenResponse>.Fail(AccountStatusMessages.Deactivated);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = GenerateAccessToken(user, roles, domainUser?.Name);
        var newRawRefreshToken = GenerateRawToken();
        var newHash = Hash(newRawRefreshToken);

        stored.RevokedAt = DateTime.UtcNow;

        _context.RefreshTokens.Add(new RefreshToken
        {
            ApplicationUserId = user.Id,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
        await _context.SaveChangesAsync();

        return IdentityResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawRefreshToken
        });
    }

    public async Task<IdentityResponse> RevokeAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || !stored.IsActive)
            return IdentityResponse.Fail("Invalid refresh token.");

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return IdentityResponse.Ok();
    }

    public async Task<IdentityResponse> RevokeAllForUserAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.ApplicationUserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return IdentityResponse.Ok();
    }

    public async Task<IdentityResponse> RevokeOtherSessionsAsync(
        string userId,
        string? currentRefreshToken = null,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.ApplicationUserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var currentHash = !string.IsNullOrEmpty(currentRefreshToken)
            ? Hash(currentRefreshToken)
            : null;

        foreach (var activeToken in activeTokens)
        {
            if (currentHash is null || activeToken.TokenHash != currentHash)
            {
                activeToken.RevokedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResponse.Ok();
    }


    private string GenerateAccessToken(ApplicationUser user, IList<string> roles, string? displayName = null)
    {
        var effectiveDisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : user.UserName ?? string.Empty;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, effectiveDisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}

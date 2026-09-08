using TalentX.Application.Identity.Models;

namespace TalentX.Application.Identity.Interfaces;

public interface ITokenService
{
    Task<IdentityResponse<TokenResponse>> CreateAsync(string userId);
    Task<IdentityResponse<TokenResponse>> ValidateAndRotateAsync(string rawToken);
    Task<IdentityResponse> RevokeAsync(string rawToken);
    Task<IdentityResponse> RevokeAllForUserAsync(string userId);
    Task<IdentityResponse> RevokeOtherSessionsAsync(string userId, string? currentRefreshToken = null, CancellationToken cancellationToken = default);
}
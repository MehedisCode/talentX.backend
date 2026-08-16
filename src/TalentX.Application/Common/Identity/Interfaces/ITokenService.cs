using TalentX.Application.Common.Identity.Models;
namespace TalentX.Application.Common.Identity.Interfaces;

public interface ITokenService
{
    Task<IdentityResponse<TokenResponse>> CreateAsync(string userId);
    Task<IdentityResponse<TokenResponse>> ValidateAndRotateAsync(string rawToken);
    Task<IdentityResponse> RevokeAsync(string rawToken);
    Task<IdentityResponse> RevokeAllForUserAsync(string userId);
}
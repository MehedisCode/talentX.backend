
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Common.Identity.Interfaces;
public interface ITokenService
{
    Task<IdentityResponse> CreateAsync(string userId);
    Task<IdentityResponse> ValidateAndRotateAsync(string rawToken);
    Task<IdentityResponse> RevokeAsync(string rawToken);
}
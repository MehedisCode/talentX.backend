using TalentX.Application.Common.Identity.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Identity.Interfaces;
public interface IIdentityService
{
    Task<IdentityResponse> CreateApplicationUserAsync(string email, string password, UserRole role);
    Task<IdentityResponse> ValidatePasswordAsync(string email, string password);
    Task<IdentityResponse> GetApplicationUserEmailByUserIdAsync(string userId);
    Task<IdentityResponse> UpdateApplicationUserEmailAsync(string userId, string newEmail);
    Task<IdentityResponse> DeleteApplicationUserAsync(string userId);

}
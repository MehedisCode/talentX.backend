using TalentX.Application.Common.Identity.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Identity.Interfaces;

public interface IIdentityService
{
    Task<IdentityResponse<CreateUserResult>> CreateApplicationUserAsync(string email, string password, UserRole role);
    Task<IdentityResponse<PasswordValidationResult>> ValidatePasswordAsync(string email, string password);
    Task<IdentityResponse<string>> GetApplicationUserEmailByUserIdAsync(string userId);
    Task<IdentityResponse> UpdateApplicationUserEmailAsync(string userId, string newEmail);
    Task<IdentityResponse> DeleteApplicationUserAsync(string userId);
}
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Identity.Interfaces;

public interface IIdentityService
{
    Task<IdentityResponse<CreateUserResult>> CreateApplicationUserAsync(string email, string password, UserRole role);
    Task<IdentityResponse<PasswordValidationResult>> ValidatePasswordAsync(string email, string password);
    Task<IdentityResponse<string>> GetApplicationUserEmailByUserIdAsync(string userId);
    Task<IdentityResponse> UpdateApplicationUserEmailAsync(string userId, string newEmail);
    Task<IdentityResponse> DeleteApplicationUserAsync(string userId);
    Task<IdentityResponse> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<IdentityResponse> AddPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
    Task<IdentityResponse> ChangeUserRoleAsync(string userId, string currentRole, string newRole);
    Task<UserAuthStatusResponse> GetUserAuthStatusAsync(string userId, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResult> InitiatePasswordResetAsync(string email, string frontendBaseUrl, CancellationToken cancellationToken = default);
    Task<IdentityResponse> ResetPasswordWithTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
}
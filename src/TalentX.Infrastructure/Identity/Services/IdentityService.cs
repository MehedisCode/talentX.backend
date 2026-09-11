using Microsoft.AspNetCore.Identity;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<IdentityResponse<CreateUserResult>> CreateApplicationUserAsync(
        string email,
        string password,
        UserRole role)
    {
        email = email.Trim();
        password = password.Trim();

        if (!await _roleManager.RoleExistsAsync(role.ToString()))
        {
            return IdentityResponse<CreateUserResult>.Fail("Invalid user role.");
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return IdentityResponse<CreateUserResult>.Fail("Email is already in use.");
        }

        var user = new ApplicationUser { UserName = email, Email = email };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return IdentityResponse<CreateUserResult>.Fail(createResult.Errors.Select(error => error.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return IdentityResponse<CreateUserResult>.Fail(roleResult.Errors.Select(error => error.Description));
        }

        return IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = user.Id });
    }

    public async Task<IdentityResponse<PasswordValidationResult>> ValidatePasswordAsync(
        string email,
        string password)
    {
        email = email.Trim();
        password = password.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return IdentityResponse<PasswordValidationResult>.Fail("Invalid credentials.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return IdentityResponse<PasswordValidationResult>.Fail("Account is locked out. Try again later.");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return IdentityResponse<PasswordValidationResult>.Fail("Invalid credentials.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return IdentityResponse<PasswordValidationResult>.Ok(new PasswordValidationResult
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = roles
        });
    }

    public async Task<IdentityResponse<string>> GetApplicationUserEmailByUserIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse<string>.Fail("User not found.");
        }

        return IdentityResponse<string>.Ok(user.Email!);
    }

    public async Task<IdentityResponse> UpdateApplicationUserEmailAsync(string userId, string newEmail)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse.Fail("User not found.");
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var changeResult = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!changeResult.Succeeded)
        {
            return IdentityResponse.Fail(changeResult.Errors.Select(error => error.Description));
        }

        var setUserNameResult = await _userManager.SetUserNameAsync(user, newEmail);
        if (!setUserNameResult.Succeeded)
        {
            return IdentityResponse.Fail(setUserNameResult.Errors.Select(error => error.Description));
        }

        return IdentityResponse.Ok();
    }

    public async Task<IdentityResponse> DeleteApplicationUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse.Fail("User not found.");
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? IdentityResponse.Ok()
            : IdentityResponse.Fail(result.Errors.Select(error => error.Description));
    }

    public async Task<IdentityResponse> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        currentPassword = currentPassword.Trim();
        newPassword = newPassword.Trim();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse.Fail("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            bool isPasswordMismatch = result.Errors.Any(e => e.Code == "PasswordMismatch");

            if (isPasswordMismatch)
            {
                return IdentityResponse.Fail("Incorrect password.");
            }

            return IdentityResponse.Fail(result.Errors.Select(error => error.Description));
        }

        return IdentityResponse.Ok();
    }

    public async Task<IdentityResponse> AddPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse.Fail("User not found.");
        }

        if (await _userManager.HasPasswordAsync(user))
        {
            return IdentityResponse.Fail("User already has a password set. Use change password instead.");
        }

        var identityResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!identityResult.Succeeded)
        {
            return IdentityResponse.Fail(identityResult.Errors.Select(identityError => identityError.Description));
        }

        await _userManager.UpdateSecurityStampAsync(user);

        return IdentityResponse.Ok();
    }

    public async Task<IdentityResponse> ChangeUserRoleAsync(string userId, string currentRole, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityResponse.Fail("User not found.");
        }

        if (!await _roleManager.RoleExistsAsync(newRole))
        {
            return IdentityResponse.Fail($"Role '{newRole}' does not exist.");
        }

        if (await _userManager.IsInRoleAsync(user, currentRole))
        {
            var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
            if (!removeResult.Succeeded)
            {
                return IdentityResponse.Fail(removeResult.Errors.Select(error => error.Description));
            }
        }

        if (!await _userManager.IsInRoleAsync(user, newRole))
        {
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                return IdentityResponse.Fail(addResult.Errors.Select(error => error.Description));
            }
        }

        return IdentityResponse.Ok();
    }

    public async Task<UserAuthStatusResponse> GetUserAuthStatusAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new UserAuthStatusResponse(false, []);
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);
        var providers = logins
            .Select(login => login.LoginProvider.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (hasPassword && !providers.Contains("password"))
        {
            providers.Add("password");
        }

        if (!hasPassword && providers.Count == 0)
        {
            providers.Add("google");
        }

        return new UserAuthStatusResponse(hasPassword, providers);
    }

    public async Task<ForgotPasswordResult> InitiatePasswordResetAsync(
        string email,
        string frontendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new ForgotPasswordResult(true, null, "If an account exists, instructions have been sent.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);

        if (!hasPassword && logins.Any(login => login.LoginProvider.Equals("Google", StringComparison.OrdinalIgnoreCase)))
        {
            return new ForgotPasswordResult(false, "Google", "This account is registered with Google. Please sign in with Google.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, user.UserName ?? "User", resetUrl, cancellationToken);

        return new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");
    }

    public async Task<IdentityResponse> ResetPasswordWithTokenAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        newPassword = newPassword.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return IdentityResponse.Fail("Invalid or expired password reset token.");
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!resetResult.Succeeded)
        {
            return IdentityResponse.Fail("Invalid or expired password reset token.");
        }

        var isGuest = await _userManager.IsInRoleAsync(user, UserRole.Guest.ToString());
        if (isGuest)
        {
            await _userManager.RemoveFromRoleAsync(user, UserRole.Guest.ToString());
            if (!await _userManager.IsInRoleAsync(user, UserRole.Talent.ToString()))
            {
                await _userManager.AddToRoleAsync(user, UserRole.Talent.ToString());
            }
        }

        await _tokenService.RevokeAllForUserAsync(user.Id);

        return IdentityResponse.Ok();
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return false;
        }

        var tokenProvider = _userManager.Options?.Tokens?.PasswordResetTokenProvider ?? TokenOptions.DefaultProvider;

        return await _userManager.VerifyUserTokenAsync(
            user,
            tokenProvider,
            UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
            token);
    }
}

using Microsoft.AspNetCore.Identity;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IdentityResponse<CreateUserResult>> CreateApplicationUserAsync(
        string email,
        string password,
        UserRole role
    )
    {
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
            return IdentityResponse<CreateUserResult>.Fail(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return IdentityResponse<CreateUserResult>.Fail(roleResult.Errors.Select(e => e.Description));
        }

        return IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = user.Id });
    }

    public async Task<IdentityResponse<PasswordValidationResult>> ValidatePasswordAsync(
        string email,
        string password
    )
    {
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
            return IdentityResponse.Fail(changeResult.Errors.Select(e => e.Description));
        }

        var setUserNameResult = await _userManager.SetUserNameAsync(user, newEmail);
        if (!setUserNameResult.Succeeded)
        {
            return IdentityResponse.Fail(setUserNameResult.Errors.Select(e => e.Description));
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
            : IdentityResponse.Fail(result.Errors.Select(e => e.Description));
    }
}

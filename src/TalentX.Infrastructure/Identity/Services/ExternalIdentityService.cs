using Microsoft.AspNetCore.Identity;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Services;

public sealed class ExternalIdentityService : IExternalIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ExternalIdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool IsNewUser, string UserId)> FindOrCreateExternalUserAsync(
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        return await FindOrCreateExternalUserAsync(email, role, null, cancellationToken);
    }

    public async Task<(bool IsNewUser, string UserId)> FindOrCreateExternalUserAsync(
        string email,
        string role,
        string? providerKey,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            await EnsureGoogleLoginAsync(existingUser, providerKey ?? email);
            return (IsNewUser: false, UserId: existingUser.Id);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(identityError => identityError.Description));
            throw new InvalidOperationException($"Failed to provision external identity user: {errors}");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
        }

        await EnsureGoogleLoginAsync(user, providerKey ?? email);

        return (IsNewUser: true, UserId: user.Id);
    }

    private async Task EnsureGoogleLoginAsync(ApplicationUser user, string providerKey)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        var hasGoogleLogin = logins.Any(login =>
            string.Equals(login.LoginProvider, "Google", StringComparison.OrdinalIgnoreCase));

        if (!hasGoogleLogin)
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", providerKey, "Google"));
        }
    }
}

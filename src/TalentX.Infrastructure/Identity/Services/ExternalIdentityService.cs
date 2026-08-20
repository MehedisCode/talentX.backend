using Microsoft.AspNetCore.Identity;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Services;

/// <summary>
/// Implementation of <see cref="IExternalIdentityService"/> using ASP.NET Core Identity for passwordless external OAuth accounts.
/// </summary>
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

    /// <inheritdoc/>
    public async Task<(bool IsNewUser, string UserId)> FindOrCreateExternalUserAsync(
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
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
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
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

        return (IsNewUser: true, UserId: user.Id);
    }
}

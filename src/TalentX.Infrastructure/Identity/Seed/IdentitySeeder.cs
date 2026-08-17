using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        var roles = new[]
        {
            UserRole.Employer,
            UserRole.Talent
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.ToString()))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role.ToString()));
            }
        }
    }
}
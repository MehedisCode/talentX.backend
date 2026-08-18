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

        // 1. Seed Roles
        var roles = new[]
        {
            UserRole.Employer,
            UserRole.Talent
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(role.ToString()));
            }
        }

        // 2. Seed Test Employer User
        const string testEmail = "employer@talentx.com";
        const string testPassword = "Password123!"; // Must satisfy Identity password rules

        var existingUser = await userManager.FindByEmailAsync(testEmail);

        if (existingUser == null)
        {
            var employerUser = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true // Bypasses email confirmation requirement for testing
            };

            // Create user with hashed password
            var result = await userManager.CreateAsync(employerUser, testPassword);

            if (result.Succeeded)
            {
                // Assign Employer role
                await userManager.AddToRoleAsync(employerUser, UserRole.Employer.ToString());
            }
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Infrastructure.Identity.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var configuration = scope.ServiceProvider.GetService<IConfiguration>();

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
            UserRole.Talent,
            UserRole.Guest,
            UserRole.Admin
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.ToString()))
            {
                await roleManager.CreateAsync(new IdentityRole(role.ToString()));
            }
        }

        // 2. Seed Super Admin User from Configuration / Environment
        var adminEmail = configuration?["ADMIN_EMAIL"]
            ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = configuration?["ADMIN_PASS"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASS");

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, UserRole.Admin.ToString()))
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
            }

            var domainAdmin = await unitOfWork.Users.GetByEmailAsync(adminEmail);
            if (domainAdmin == null)
            {
                domainAdmin = new User(adminUser.Id, "Super Admin", adminEmail, UserRole.Admin, isVerified: true);
                await unitOfWork.Users.AddAsync(domainAdmin);
                await unitOfWork.SaveChangesAsync();
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

        // 3. Seed Test Talent User
        const string talentEmail = "talent@talentx.com";

        var talentUser = await userManager.FindByEmailAsync(talentEmail);

        if (talentUser == null)
        {
            talentUser = new ApplicationUser
            {
                UserName = talentEmail,
                Email = talentEmail,
                EmailConfirmed = true
            };

            var talentResult = await userManager.CreateAsync(talentUser, testPassword);

            if (talentResult.Succeeded)
            {
                await userManager.AddToRoleAsync(talentUser, UserRole.Talent.ToString());
            }
        }

        
        if (await userManager.IsInRoleAsync(talentUser, UserRole.Talent.ToString()))
        {
            if (await unitOfWork.Talents.GetByUserIdAsync(talentUser.Id) == null)
            {
                var talent = new Talent(talentUser.Id, "Test Talent", talentEmail);
                talent.UpdateProfile(
                    name: "Test Talent",
                    tagline: "Full-stack engineer",
                    shortBio: "Seed account for local testing of the apply flow.",
                    location: "Dhaka, Bangladesh",
                    phone: null,
                    linkedInUrl: null,
                    additionalUrls: null,
                    totalYearsExperience: 5m,
                    expectedSalary: 90000m,
                    skillPropositions: null);

                await unitOfWork.Talents.AddAsync(talent);
            }

            if (await unitOfWork.Resumes.GetByUserIdAsync(talentUser.Id) == null)
            {
                var resume = new Resume(
                    talentUser.Id,
                    "resumes/seed/test-talent-resume.pdf",
                    "test-talent-resume.pdf",
                    "application/pdf",
                    123456);

                await unitOfWork.Resumes.AddAsync(resume);
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}
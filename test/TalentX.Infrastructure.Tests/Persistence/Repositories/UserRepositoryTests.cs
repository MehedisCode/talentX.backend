using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class UserRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByEmailAsync_PersistsAndRetrievesUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User("admin-user-id", "Super Admin", "superadmin@ait.com", UserRole.Admin, isVerified: true);

        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        var retrievedUser = await repository.GetByEmailAsync("superadmin@ait.com");

        retrievedUser.Should().NotBeNull();
        retrievedUser!.Email.Should().Be("superadmin@ait.com");
        retrievedUser.Name.Should().Be("Super Admin");
        retrievedUser.Role.Should().Be(UserRole.Admin);
        retrievedUser.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMatchingUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User("admin-user-id-2", "Admin User", "admin@ait.com", UserRole.Admin, isVerified: true);

        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        var retrievedUser = await repository.GetByUserIdAsync("admin-user-id-2");
        var nonExistentUser = await repository.GetByUserIdAsync("non-existent-id");

        retrievedUser.Should().NotBeNull();
        retrievedUser!.UserId.Should().Be("admin-user-id-2");
        nonExistentUser.Should().BeNull();
    }

    [Fact]
    public async Task Update_UpdatesExistingUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User("user-to-update", "Original Name", "user@ait.com", UserRole.Employer, isVerified: false);

        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        user.UpdateName("Updated Name");
        user.MarkAsVerified();
        repository.Update(user);
        await context.SaveChangesAsync();

        var retrievedUser = await repository.GetByUserIdAsync("user-to-update");

        retrievedUser.Should().NotBeNull();
        retrievedUser!.Name.Should().Be("Updated Name");
        retrievedUser.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new Talent("talent-by-id", "Talent By Id", "talent-by-id@ait.com");

        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        var retrievedUser = await repository.GetByIdAsync(user.Id);
        var nonExistentUser = await repository.GetByIdAsync(Guid.NewGuid());

        retrievedUser.Should().NotBeNull();
        retrievedUser!.Id.Should().Be(user.Id);
        nonExistentUser.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesAdminUsersRegardlessOfFilter()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        await repository.AddAsync(new User("admin-1", "Admin One", "admin1@ait.com", UserRole.Admin, isVerified: true));
        await repository.AddAsync(new Talent("talent-1", "Talent One", "talent1@ait.com"));
        await context.SaveChangesAsync();

        var (items, totalCount) = await repository.GetPagedAsync(new UserQueryParams());

        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Role.Should().Be(UserRole.Talent);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByRoleAndStatus()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        var employer = new Employer("employer-1", "Employer One", "employer1@ait.com", "Acme Inc");
        var deactivatedTalent = new Talent("talent-2", "Talent Two", "talent2@ait.com");
        deactivatedTalent.Deactivate();

        await repository.AddAsync(employer);
        await repository.AddAsync(deactivatedTalent);
        await context.SaveChangesAsync();

        var (employerOnly, _) = await repository.GetPagedAsync(new UserQueryParams { Role = UserRole.Employer });
        var (deactivatedOnly, _) = await repository.GetPagedAsync(new UserQueryParams { Status = AccountStatus.Deactivated });

        employerOnly.Should().ContainSingle().Which.Email.Should().Be("employer1@ait.com");
        deactivatedOnly.Should().ContainSingle().Which.Email.Should().Be("talent2@ait.com");
    }

    [Fact]
    public async Task GetPagedAsync_SearchesByNameOrEmailCaseInsensitively()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        await repository.AddAsync(new Talent("talent-3", "Jordan Smith", "jordan.smith@ait.com"));
        await repository.AddAsync(new Talent("talent-4", "Alex Rivera", "alex.rivera@ait.com"));
        await context.SaveChangesAsync();

        var (byName, _) = await repository.GetPagedAsync(new UserQueryParams { SearchQuery = "jordan" });
        var (byEmail, _) = await repository.GetPagedAsync(new UserQueryParams { SearchQuery = "RIVERA" });

        byName.Should().ContainSingle().Which.Name.Should().Be("Jordan Smith");
        byEmail.Should().ContainSingle().Which.Name.Should().Be("Alex Rivera");
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesResults()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        for (var i = 0; i < 3; i++)
        {
            await repository.AddAsync(new Talent($"talent-page-{i}", $"Talent Page {i}", $"talent-page-{i}@ait.com"));
        }

        await context.SaveChangesAsync();

        var (firstPage, totalCount) = await repository.GetPagedAsync(new UserQueryParams { Page = 1, PageSize = 2 });
        var (secondPage, _) = await repository.GetPagedAsync(new UserQueryParams { Page = 2, PageSize = 2 });

        totalCount.Should().Be(3);
        firstPage.Should().HaveCount(2);
        secondPage.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRoleAndStatusCountsAsync_ExcludesAdminsAndGroupsCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        var deactivatedTalent = new Talent("talent-counts-1", "Talent One", "talent-counts-1@ait.com");
        deactivatedTalent.Deactivate();

        await repository.AddAsync(new User("admin-counts", "Admin", "admin-counts@ait.com", UserRole.Admin, isVerified: true));
        await repository.AddAsync(deactivatedTalent);
        await repository.AddAsync(new Talent("talent-counts-2", "Talent Two", "talent-counts-2@ait.com"));
        await repository.AddAsync(new Employer("employer-counts-1", "Employer One", "employer-counts-1@ait.com", "Acme Inc"));
        await context.SaveChangesAsync();

        var (roleCounts, statusCounts) = await repository.GetRoleAndStatusCountsAsync();

        roleCounts.Should().NotContainKey(UserRole.Admin);
        roleCounts[UserRole.Talent].Should().Be(2);
        roleCounts[UserRole.Employer].Should().Be(1);
        statusCounts[AccountStatus.Active].Should().Be(2);
        statusCounts[AccountStatus.Deactivated].Should().Be(1);
    }
}

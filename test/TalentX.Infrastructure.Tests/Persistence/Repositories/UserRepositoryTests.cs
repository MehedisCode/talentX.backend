using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
}

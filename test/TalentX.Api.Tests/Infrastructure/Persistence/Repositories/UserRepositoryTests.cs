using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Api.Tests.Infrastructure.Persistence.Repositories;

public class UserRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsIt()
    {
        await using var context = NewContext();
        var repository = new UserRepository(context);
        var user = new User("user-1", "Jane Doe", "jane@example.com");
        context.DomainUsers.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmailAsync("jane@example.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByEmailAsync("missing@example.com");

        result.Should().BeNull();
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class TalentRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_PersistsAndRetrievesTalent()
    {
        await using var context = CreateInMemoryContext();
        var repository = new TalentRepository(context);
        var talent = new Talent("user-1", "Alice Talent", "alice@example.com");

        await repository.AddAsync(talent);
        await context.SaveChangesAsync();

        var retrievedTalent = await repository.GetByIdAsync(talent.Id);

        retrievedTalent.Should().NotBeNull();
        retrievedTalent!.Id.Should().Be(talent.Id);
        retrievedTalent.UserId.Should().Be("user-1");
        retrievedTalent.Name.Should().Be("Alice Talent");
        retrievedTalent.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMatchingTalent()
    {
        await using var context = CreateInMemoryContext();
        var repository = new TalentRepository(context);
        var talent = new Talent("user-2", "Bob Talent", "bob@example.com");

        await repository.AddAsync(talent);
        await context.SaveChangesAsync();

        var retrievedTalent = await repository.GetByUserIdAsync("user-2");
        var nonExistentTalent = await repository.GetByUserIdAsync("unknown-user");

        retrievedTalent.Should().NotBeNull();
        retrievedTalent!.Email.Should().Be("bob@example.com");
        nonExistentTalent.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsMatchingTalent()
    {
        await using var context = CreateInMemoryContext();
        var repository = new TalentRepository(context);
        var talent = new Talent("user-3", "Charlie Talent", "charlie@example.com");

        await repository.AddAsync(talent);
        await context.SaveChangesAsync();

        var retrievedTalent = await repository.GetByEmailAsync("charlie@example.com");
        var nonExistentTalent = await repository.GetByEmailAsync("nobody@example.com");

        retrievedTalent.Should().NotBeNull();
        retrievedTalent!.UserId.Should().Be("user-3");
        nonExistentTalent.Should().BeNull();
    }

    [Fact]
    public async Task Update_UpdatesExistingTalent()
    {
        await using var context = CreateInMemoryContext();
        var repository = new TalentRepository(context);
        var talent = new Talent("user-4", "Diana Prince", "diana@example.com");

        await repository.AddAsync(talent);
        await context.SaveChangesAsync();

        talent.UpdateProfile(
            name: "Diana Prince",
            tagline: "Senior Cloud Architect",
            shortBio: "Cloud native specialist",
            location: "London, UK",
            phone: "+447000000000",
            linkedInUrl: "https://linkedin.com/in/dianaprince",
            additionalUrls: new List<string> { "https://diana.dev" },
            totalYearsExperience: 10m,
            expectedSalary: 150000m,
            skillPropositions: new List<string> { "AWS", "Kubernetes" });

        repository.Update(talent);
        await context.SaveChangesAsync();

        var updatedTalent = await repository.GetByIdAsync(talent.Id);

        updatedTalent.Should().NotBeNull();
        updatedTalent!.Tagline.Should().Be("Senior Cloud Architect");
        updatedTalent.TotalYearsExperience.Should().Be(10m);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class WorkExperienceRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByUserIdAsync_ReturnsAllUserExperiences()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);
        var experience1 = new WorkExperience("user-1", "Company A", "Engineer", "2020", "2021", false);
        var experience2 = new WorkExperience("user-1", "Company B", "Senior Engineer", "2021", null, true);
        var otherUserExperience = new WorkExperience("user-2", "Company C", "Lead", "2019", "2022", false);

        await repository.AddAsync(experience1);
        await repository.AddAsync(experience2);
        await repository.AddAsync(otherUserExperience);
        await context.SaveChangesAsync();

        var experiences = await repository.GetByUserIdAsync("user-1");

        experiences.Should().HaveCount(2);
        experiences.Select(experience => experience.CompanyName).Should().Contain(new[] { "Company A", "Company B" });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingExperience()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);
        var experience = new WorkExperience("user-1", "Company A", "Engineer", "2020", "2021", false);

        await repository.AddAsync(experience);
        await context.SaveChangesAsync();

        var retrievedExperience = await repository.GetByIdAsync(experience.Id);
        var nonExistentExperience = await repository.GetByIdAsync(Guid.NewGuid());

        retrievedExperience.Should().NotBeNull();
        retrievedExperience!.JobTitle.Should().Be("Engineer");
        nonExistentExperience.Should().BeNull();
    }

    [Fact]
    public async Task Update_UpdatesExistingExperience()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);
        var experience = new WorkExperience("user-1", "Company A", "Engineer", "2020", "2021", false);

        await repository.AddAsync(experience);
        await context.SaveChangesAsync();

        experience.Update("Company A", "Staff Engineer", "2020", "2022", false);
        repository.Update(experience);
        await context.SaveChangesAsync();

        var updatedExperience = await repository.GetByIdAsync(experience.Id);

        updatedExperience.Should().NotBeNull();
        updatedExperience!.JobTitle.Should().Be("Staff Engineer");
        updatedExperience.EndDate.Should().Be("2022");
    }

    [Fact]
    public async Task Delete_RemovesSingleExperience()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);
        var experience = new WorkExperience("user-1", "Company A", "Engineer", "2020", "2021", false);

        await repository.AddAsync(experience);
        await context.SaveChangesAsync();

        repository.Delete(experience);
        await context.SaveChangesAsync();

        var retrievedExperience = await repository.GetByIdAsync(experience.Id);

        retrievedExperience.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByUserIdAsync_RemovesAllExperiencesForUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);
        var experience1 = new WorkExperience("user-1", "Company A", "Engineer", "2020", "2021", false);
        var experience2 = new WorkExperience("user-1", "Company B", "Senior Engineer", "2021", null, true);
        var otherUserExperience = new WorkExperience("user-2", "Company C", "Lead", "2019", "2022", false);

        await repository.AddAsync(experience1);
        await repository.AddAsync(experience2);
        await repository.AddAsync(otherUserExperience);
        await context.SaveChangesAsync();

        await repository.DeleteByUserIdAsync("user-1");
        await context.SaveChangesAsync();

        var userExperiences = await repository.GetByUserIdAsync("user-1");
        var otherExperiences = await repository.GetByUserIdAsync("user-2");

        userExperiences.Should().BeEmpty();
        otherExperiences.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsExperiencesOrderedByStartDateDescendingThenId()
    {
        await using var context = CreateInMemoryContext();
        var repository = new WorkExperienceRepository(context);

        var oldest = new WorkExperience("user-1", "Company A", "Engineer", "2016", "2018", false);
        var newest = new WorkExperience("user-1", "Company C", "Lead", "2022", null, true);
        var middle = new WorkExperience("user-1", "Company B", "Senior Engineer", "2020", "2022", false);
        var tiedOlder = new WorkExperience("user-1", "Company D", "Junior", "2016", "2017", false);

        await repository.AddAsync(oldest);
        await repository.AddAsync(newest);
        await repository.AddAsync(middle);
        await repository.AddAsync(tiedOlder);
        await context.SaveChangesAsync();

        var experiences = await repository.GetByUserIdAsync("user-1");

        experiences.Should().HaveCount(4);
        experiences[0].Id.Should().Be(newest.Id);
        experiences[1].Id.Should().Be(middle.Id);

        var expectedTieOrder = new[] { oldest.Id, tiedOlder.Id }.OrderBy(id => id).ToArray();
        experiences[2].Id.Should().Be(expectedTieOrder[0]);
        experiences[3].Id.Should().Be(expectedTieOrder[1]);
    }
}

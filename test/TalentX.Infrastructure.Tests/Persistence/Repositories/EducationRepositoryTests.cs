using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class EducationRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByUserIdAsync_ReturnsAllUserEducations()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EducationRepository(context);
        var education1 = new Education("user-1", "University A", "B.Sc.", "2015", "2019");
        var education2 = new Education("user-1", "University B", "M.Sc.", "2019", "2021");
        var otherUserEducation = new Education("user-2", "University C", "Ph.D.", "2016", "2020");

        await repository.AddAsync(education1);
        await repository.AddAsync(education2);
        await repository.AddAsync(otherUserEducation);
        await context.SaveChangesAsync();

        var educations = await repository.GetByUserIdAsync("user-1");

        educations.Should().HaveCount(2);
        educations.Select(education => education.Institution).Should().Contain(new[] { "University A", "University B" });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingEducation()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EducationRepository(context);
        var education = new Education("user-1", "University A", "B.Sc.", "2015", "2019");

        await repository.AddAsync(education);
        await context.SaveChangesAsync();

        var retrievedEducation = await repository.GetByIdAsync(education.Id);
        var nonExistentEducation = await repository.GetByIdAsync(Guid.NewGuid());

        retrievedEducation.Should().NotBeNull();
        retrievedEducation!.Degree.Should().Be("B.Sc.");
        nonExistentEducation.Should().BeNull();
    }

    [Fact]
    public async Task Update_UpdatesExistingEducation()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EducationRepository(context);
        var education = new Education("user-1", "University A", "B.Sc.", "2015", "2019");

        await repository.AddAsync(education);
        await context.SaveChangesAsync();

        education.Update("University A", "B.Sc. with Honors", "2015", "2019");
        repository.Update(education);
        await context.SaveChangesAsync();

        var updatedEducation = await repository.GetByIdAsync(education.Id);

        updatedEducation.Should().NotBeNull();
        updatedEducation!.Degree.Should().Be("B.Sc. with Honors");
    }

    [Fact]
    public async Task Delete_RemovesSingleEducation()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EducationRepository(context);
        var education = new Education("user-1", "University A", "B.Sc.", "2015", "2019");

        await repository.AddAsync(education);
        await context.SaveChangesAsync();

        repository.Delete(education);
        await context.SaveChangesAsync();

        var retrievedEducation = await repository.GetByIdAsync(education.Id);

        retrievedEducation.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByUserIdAsync_RemovesAllEducationsForUser()
    {
        await using var context = CreateInMemoryContext();
        var repository = new EducationRepository(context);
        var education1 = new Education("user-1", "University A", "B.Sc.", "2015", "2019");
        var education2 = new Education("user-1", "University B", "M.Sc.", "2019", "2021");
        var otherUserEducation = new Education("user-2", "University C", "Ph.D.", "2016", "2020");

        await repository.AddAsync(education1);
        await repository.AddAsync(education2);
        await repository.AddAsync(otherUserEducation);
        await context.SaveChangesAsync();

        await repository.DeleteByUserIdAsync("user-1");
        await context.SaveChangesAsync();

        var userEducations = await repository.GetByUserIdAsync("user-1");
        var otherEducations = await repository.GetByUserIdAsync("user-2");

        userEducations.Should().BeEmpty();
        otherEducations.Should().HaveCount(1);
    }
}

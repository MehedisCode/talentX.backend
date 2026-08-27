using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class ResumeRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByUserIdAsync_PersistsAndRetrievesResume()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        var resume = new Resume("user-1", "resumes/user-1.pdf", "user-1.pdf", "application/pdf", 1024);

        await repository.AddAsync(resume);
        await context.SaveChangesAsync();

        var retrievedResume = await repository.GetByUserIdAsync("user-1");

        retrievedResume.Should().NotBeNull();
        retrievedResume!.UserId.Should().Be("user-1");
        retrievedResume.StorageKey.Should().Be("resumes/user-1.pdf");
        retrievedResume.OriginalFileName.Should().Be("user-1.pdf");
        retrievedResume.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public async Task Update_UpdatesExistingResume()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        var resume = new Resume("user-2", "resumes/old.pdf", "old.pdf", "application/pdf", 1024);

        await repository.AddAsync(resume);
        await context.SaveChangesAsync();

        resume.UpdateFile("resumes/new.pdf", "new.pdf", "application/pdf", 2048);
        repository.Update(resume);
        await context.SaveChangesAsync();

        var updatedResume = await repository.GetByUserIdAsync("user-2");

        updatedResume.Should().NotBeNull();
        updatedResume!.StorageKey.Should().Be("resumes/new.pdf");
        updatedResume.OriginalFileName.Should().Be("new.pdf");
        updatedResume.SizeBytes.Should().Be(2048);
    }

    [Fact]
    public async Task Delete_RemovesResumeFromDatabase()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        var resume = new Resume("user-3", "resumes/user-3.pdf", "user-3.pdf", "application/pdf", 1024);

        await repository.AddAsync(resume);
        await context.SaveChangesAsync();

        repository.Delete(resume);
        await context.SaveChangesAsync();

        var retrievedResume = await repository.GetByUserIdAsync("user-3");

        retrievedResume.Should().BeNull();
    }
}

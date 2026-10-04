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
    public async Task GetAllByUserIdAsync_ReturnsDefaultFirstThenNewestFirst()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        var first = new Resume("user-4", "resumes/a.pdf", "a.pdf", "application/pdf", 1024);
        var second = new Resume("user-4", "resumes/b.pdf", "b.pdf", "application/pdf", 1024);
        var otherUser = new Resume("user-5", "resumes/c.pdf", "c.pdf", "application/pdf", 1024);
        second.SetDefault();

        await repository.AddAsync(first);
        await repository.AddAsync(second);
        await repository.AddAsync(otherUser);
        await context.SaveChangesAsync();

        var resumes = await repository.GetAllByUserIdAsync("user-4");

        resumes.Should().HaveCount(2);
        resumes[0].Id.Should().Be(second.Id);
        resumes[1].Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task GetDefaultByUserIdAsync_ReturnsTheDefaultResume()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        var plain = new Resume("user-6", "resumes/a.pdf", "a.pdf", "application/pdf", 1024);
        var preferred = new Resume("user-6", "resumes/b.pdf", "b.pdf", "application/pdf", 1024);
        preferred.SetDefault();

        await repository.AddAsync(plain);
        await repository.AddAsync(preferred);
        await context.SaveChangesAsync();

        var resume = await repository.GetDefaultByUserIdAsync("user-6");

        resume.Should().NotBeNull();
        resume!.Id.Should().Be(preferred.Id);
    }

    [Fact]
    public async Task CountByUserIdAsync_CountsOnlyThatUsersResumes()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        await repository.AddAsync(new Resume("user-7", "resumes/a.pdf", "a.pdf", "application/pdf", 1024));
        await repository.AddAsync(new Resume("user-7", "resumes/b.pdf", "b.pdf", "application/pdf", 1024));
        await repository.AddAsync(new Resume("user-8", "resumes/c.pdf", "c.pdf", "application/pdf", 1024));
        await context.SaveChangesAsync();

        var count = await repository.CountByUserIdAsync("user-7");

        count.Should().Be(2);
    }

    [Fact]
    public async Task ExistsByStorageKeyAsync_ReturnsTrueOnlyForAMatchingKey()
    {
        await using var context = CreateInMemoryContext();
        var repository = new ResumeRepository(context);
        await repository.AddAsync(new Resume("user-9", "resumes/user-9/a.pdf", "a.pdf", "application/pdf", 1024));
        await context.SaveChangesAsync();

        var exists = await repository.ExistsByStorageKeyAsync("resumes/user-9/a.pdf");
        var missing = await repository.ExistsByStorageKeyAsync("resumes/user-9/other.pdf");

        exists.Should().BeTrue();
        missing.Should().BeFalse();
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

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Tests.Infrastructure.Persistence.Repositories;

public class JobRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job NewJob()
    {
        return Job.CreateDraft(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Remote,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenJobExists_ReturnsIt()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = NewJob();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(job.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenJobDoesNotExist_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_TracksTheJobForInsertion()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = NewJob();

        await repository.AddAsync(job);
        await context.SaveChangesAsync();

        (await context.Jobs.FindAsync(job.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_AssignsAGeneratedIdOnceTracked()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = NewJob();
        job.Id.Should().Be(Guid.Empty);

        await repository.AddAsync(job);

        job.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Update_MarksTheJobAsModified()
    {
        using var context = NewContext();
        var repository = new JobRepository(context);
        var job = NewJob();
        context.Jobs.Add(job);
        context.SaveChanges();
        context.Entry(job).State = EntityState.Detached;

        job.UpdateDetails("Updated Title", null, null, null, null, null, null, null, null, null);
        repository.Update(job);

        context.Entry(job).State.Should().Be(EntityState.Modified);
    }
}

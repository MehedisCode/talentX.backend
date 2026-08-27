using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobApplicationRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static JobApplication NewApplication(Guid? jobId = null, string talentUserId = "talent-1")
    {
        return new JobApplication(
            jobId ?? Guid.NewGuid(),
            talentUserId,
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf",
            "Cover letter text",
            9000m,
            4.0m,
            "Remote");
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_PersistsAndRetrievesApplication()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var application = NewApplication();

        await repository.AddAsync(application);
        await context.SaveChangesAsync();

        var retrievedApplication = await repository.GetByIdAsync(application.Id);

        retrievedApplication.Should().NotBeNull();
        retrievedApplication!.Id.Should().Be(application.Id);
        retrievedApplication.ApplicantName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task ExistsForJobAndTalentAsync_ReturnsCorrectStatus()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var targetJobId = Guid.NewGuid();
        var application = NewApplication(jobId: targetJobId, talentUserId: "talent-specific");

        await repository.AddAsync(application);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsForJobAndTalentAsync(targetJobId, "talent-specific");
        var notExistsDifferentTalent = await repository.ExistsForJobAndTalentAsync(targetJobId, "talent-other");
        var notExistsDifferentJob = await repository.ExistsForJobAndTalentAsync(Guid.NewGuid(), "talent-specific");

        exists.Should().BeTrue();
        notExistsDifferentTalent.Should().BeFalse();
        notExistsDifferentJob.Should().BeFalse();
    }

    [Fact]
    public async Task GetByJobIdAsync_ReturnsApplicationsOnlyForTargetJob()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var targetJobId = Guid.NewGuid();
        var application1 = NewApplication(jobId: targetJobId, talentUserId: "talent-1");
        var application2 = NewApplication(jobId: targetJobId, talentUserId: "talent-2");
        var application3 = NewApplication(jobId: Guid.NewGuid(), talentUserId: "talent-3");

        await repository.AddAsync(application1);
        await repository.AddAsync(application2);
        await repository.AddAsync(application3);
        await context.SaveChangesAsync();

        var applications = await repository.GetByJobIdAsync(targetJobId);

        applications.Should().HaveCount(2);
        applications.Select(app => app.Id).Should().Contain(new[] { application1.Id, application2.Id });
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobAlertNotificationHistoryRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ExistsAsync_WhenRecordExists_ReturnsTrue()
    {
        await using var context = NewContext();
        var repository = new JobAlertNotificationHistoryRepository(context);

        var jobId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var history = new JobAlertNotificationHistory("talent-1", jobId, alertId);

        await repository.AddAsync(history);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync("talent-1", jobId);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenRecordDoesNotExist_ReturnsFalse()
    {
        await using var context = NewContext();
        var repository = new JobAlertNotificationHistoryRepository(context);

        var exists = await repository.ExistsAsync("talent-unknown", Guid.NewGuid());

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotifiedTalentIdsAsync_ReturnsOnlyNotifiedTalentsForJob()
    {
        await using var context = NewContext();
        var repository = new JobAlertNotificationHistoryRepository(context);

        var jobId = Guid.NewGuid();
        var otherJobId = Guid.NewGuid();

        await repository.AddAsync(new JobAlertNotificationHistory("talent-1", jobId, Guid.NewGuid()));
        await repository.AddAsync(new JobAlertNotificationHistory("talent-2", jobId, Guid.NewGuid()));
        await repository.AddAsync(new JobAlertNotificationHistory("talent-3", otherJobId, Guid.NewGuid()));
        await context.SaveChangesAsync();

        var candidateTalentIds = new[] { "talent-1", "talent-2", "talent-3", "talent-4" };

        var notifiedIds = await repository.GetNotifiedTalentIdsAsync(jobId, candidateTalentIds);

        notifiedIds.Should().BeEquivalentTo(new[] { "talent-1", "talent-2" });
    }

    [Fact]
    public async Task GetNotifiedTalentIdsAsync_WhenCandidateListIsEmpty_ReturnsEmptySet()
    {
        await using var context = NewContext();
        var repository = new JobAlertNotificationHistoryRepository(context);

        var notifiedIds = await repository.GetNotifiedTalentIdsAsync(Guid.NewGuid(), []);

        notifiedIds.Should().BeEmpty();
    }
}

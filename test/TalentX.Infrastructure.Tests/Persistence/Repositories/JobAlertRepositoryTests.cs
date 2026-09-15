using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobAlertRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_PersistsAndRetrievesAlert()
    {
        await using var context = NewContext();
        var repository = new JobAlertRepository(context);

        var alert = JobAlert.Create(
            "talent-1",
            "Senior Alert",
            "Engineer",
            "Remote",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            WorkArrangement.FullRemote);

        await repository.AddAsync(alert);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(alert.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Senior Alert");
        retrieved.Title.Should().Be("Engineer");
        retrieved.TalentId.Should().Be("talent-1");
        retrieved.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTalentIdAsync_And_CountByTalentIdAsync_ReturnsCorrectAlerts()
    {
        await using var context = NewContext();
        var repository = new JobAlertRepository(context);

        var alert1 = JobAlert.Create("talent-1", "Alert 1", "Developer", null, null, null, null);
        var alert2 = JobAlert.Create("talent-1", "Alert 2", "Architect", null, null, null, null);
        var alertOther = JobAlert.Create("talent-2", "Alert Other", "Designer", null, null, null, null);

        await repository.AddAsync(alert1);
        await repository.AddAsync(alert2);
        await repository.AddAsync(alertOther);
        await context.SaveChangesAsync();

        var alerts = await repository.GetByTalentIdAsync("talent-1");
        var count = await repository.CountByTalentIdAsync("talent-1");

        count.Should().Be(2);
        alerts.Should().HaveCount(2);
        alerts.Select(a => a.Name).Should().Contain(["Alert 1", "Alert 2"]);
    }

    [Fact]
    public async Task GetActiveAlertsByTalentIdAsync_ReturnsOnlyEnabledAlerts()
    {
        await using var context = NewContext();
        var repository = new JobAlertRepository(context);

        var enabledAlert = JobAlert.Create("talent-1", "Active Alert", "Developer", null, null, null, null);
        var disabledAlert = JobAlert.Create("talent-1", "Disabled Alert", "Architect", null, null, null, null);
        disabledAlert.Toggle(false);

        await repository.AddAsync(enabledAlert);
        await repository.AddAsync(disabledAlert);
        await context.SaveChangesAsync();

        var activeAlerts = await repository.GetActiveAlertsByTalentIdAsync("talent-1");

        activeAlerts.Should().ContainSingle();
        activeAlerts[0].Name.Should().Be("Active Alert");
    }

    [Fact]
    public async Task Remove_DeletesAlert()
    {
        await using var context = NewContext();
        var repository = new JobAlertRepository(context);

        var alert = JobAlert.Create("talent-1", "To Delete", "Developer", null, null, null, null);
        await repository.AddAsync(alert);
        await context.SaveChangesAsync();

        repository.Remove(alert);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(alert.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetAllActiveAlertsAsync_ReturnsOnlyEnabledAlertsAcrossAllTalents()
    {
        await using var context = NewContext();
        var repository = new JobAlertRepository(context);

        var alert1 = JobAlert.Create("talent-1", "Alert 1", "Dev", null, null, null, null);
        var alert2 = JobAlert.Create("talent-2", "Alert 2", "QA", null, null, null, null);
        var alert3 = JobAlert.Create("talent-3", "Alert 3", "DevOps", null, null, null, null);
        alert3.Toggle(false);

        await repository.AddAsync(alert1);
        await repository.AddAsync(alert2);
        await repository.AddAsync(alert3);
        await context.SaveChangesAsync();

        var activeAlerts = await repository.GetAllActiveAlertsAsync();

        activeAlerts.Should().HaveCount(2);
        activeAlerts.Select(a => a.Name).Should().Contain(["Alert 1", "Alert 2"]);
    }
}

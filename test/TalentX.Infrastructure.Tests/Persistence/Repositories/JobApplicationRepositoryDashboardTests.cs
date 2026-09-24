using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobApplicationRepositoryDashboardTests
{
    private static readonly DateTime Base = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static JobApplication AddApplication(
        ApplicationDbContext context,
        DateTime appliedAt,
        ApplicationStatus status = ApplicationStatus.Applied,
        DateTime? statusChangedAt = null,
        bool isGuest = false)
    {
        var application = new JobApplication(
            Guid.NewGuid(),
            isGuest ? null : "talent-1",
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf");

        if (!isGuest && status != ApplicationStatus.Applied)
        {
            application.UpdateStatus(status);
        }

        context.JobApplications.Add(application);
        context.Entry(application).Property(a => a.AppliedAt).CurrentValue = appliedAt;
        if (statusChangedAt.HasValue)
        {
            context.Entry(application).Property(a => a.UpdatedAt).CurrentValue = statusChangedAt;
        }

        return application;
    }

    [Fact]
    public async Task GetStatusCountsAppliedBetweenAsync_GroupsByStatusAndExcludesPendingVerificationAndOutOfRange()
    {
        await using var context = NewContext();
        AddApplication(context, Base.AddHours(-1));
        AddApplication(context, Base.AddHours(-2));
        AddApplication(context, Base.AddHours(-3), ApplicationStatus.Shortlisted);
        AddApplication(context, Base.AddHours(-4), ApplicationStatus.Invited);
        AddApplication(context, Base.AddHours(-5), ApplicationStatus.Hired);
        AddApplication(context, Base.AddHours(-6), isGuest: true);
        AddApplication(context, Base.AddDays(-20), ApplicationStatus.Rejected);
        await context.SaveChangesAsync();
        var repository = new JobApplicationRepository(context);

        var counts = await repository.GetStatusCountsAppliedBetweenAsync(Base.AddDays(-1), Base);

        counts[ApplicationStatus.Applied].Should().Be(2);
        counts[ApplicationStatus.Shortlisted].Should().Be(1);
        counts[ApplicationStatus.Invited].Should().Be(1);
        counts[ApplicationStatus.Hired].Should().Be(1);
        counts.Should().NotContainKey(ApplicationStatus.PendingVerification);
        counts.Should().NotContainKey(ApplicationStatus.Rejected);
    }

    [Fact]
    public async Task GetAppliedCountsByBucketAsync_GroupsByDayAndSkipsPendingVerification()
    {
        await using var context = NewContext();
        AddApplication(context, Base.AddDays(-1).AddHours(-3));
        AddApplication(context, Base.AddDays(-1).AddHours(2));
        AddApplication(context, Base);
        AddApplication(context, Base.AddDays(-1), isGuest: true);
        await context.SaveChangesAsync();
        var repository = new JobApplicationRepository(context);

        var buckets = await repository.GetAppliedCountsByBucketAsync(Base.AddDays(-2), Base, groupByHour: false);

        buckets.Should().HaveCount(2);
        buckets[Base.AddDays(-1).Date].Should().Be(2);
        buckets[Base.Date].Should().Be(1);
    }

    [Fact]
    public async Task GetAppliedCountsByBucketAsync_GroupsByHour()
    {
        await using var context = NewContext();
        AddApplication(context, Base.AddHours(-2).AddMinutes(5));
        AddApplication(context, Base.AddHours(-2).AddMinutes(55));
        AddApplication(context, Base.AddHours(-1));
        await context.SaveChangesAsync();
        var repository = new JobApplicationRepository(context);

        var buckets = await repository.GetAppliedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: true);

        buckets.Should().HaveCount(2);
        buckets[Base.AddHours(-2)].Should().Be(2);
        buckets[Base.AddHours(-1)].Should().Be(1);
    }

    [Fact]
    public async Task GetHiredCountsByBucketAsync_DatesHiresByUpdatedAtAndOnlyCountsHiredApplications()
    {
        await using var context = NewContext();
        AddApplication(context, Base.AddDays(-30), ApplicationStatus.Hired, statusChangedAt: Base.AddHours(-3));
        AddApplication(context, Base.AddDays(-30), ApplicationStatus.Hired, statusChangedAt: Base.AddHours(-2));
        AddApplication(context, Base.AddDays(-30), ApplicationStatus.Hired, statusChangedAt: Base.AddDays(-15));
        AddApplication(context, Base.AddDays(-30), ApplicationStatus.Rejected, statusChangedAt: Base.AddHours(-1));
        AddApplication(context, Base.AddHours(-1));
        await context.SaveChangesAsync();
        var repository = new JobApplicationRepository(context);

        var buckets = await repository.GetHiredCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: false);

        buckets.Should().ContainSingle();
        buckets[Base.Date].Should().Be(2);
    }
}

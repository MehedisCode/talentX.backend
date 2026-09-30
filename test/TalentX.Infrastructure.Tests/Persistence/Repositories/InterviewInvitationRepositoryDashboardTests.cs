using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class InterviewInvitationRepositoryDashboardTests
{
    private static readonly DateTime Base = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static InterviewInvitation AddInvitation(
        ApplicationDbContext context,
        DateTime createdAt,
        bool cancelled = false)
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            "talent-1",
            InterviewType.OnCall);

        if (cancelled)
        {
            invitation.Cancel();
        }

        context.InterviewInvitations.Add(invitation);
        context.Entry(invitation).Property(i => i.CreatedAt).CurrentValue = createdAt;
        return invitation;
    }

    [Fact]
    public async Task CountCreatedBetweenAsync_CountsInvitationsInsideTheInclusiveRangeExcludingCancelled()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddDays(-2));
        AddInvitation(context, Base.AddDays(-1));
        AddInvitation(context, Base);
        AddInvitation(context, Base.AddDays(1));
        AddInvitation(context, Base.AddDays(2));
        AddInvitation(context, Base, cancelled: true);
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);

        var count = await repository.CountCreatedBetweenAsync(Base.AddDays(-1), Base.AddDays(1));

        count.Should().Be(3);
    }

    [Fact]
    public async Task CountCreatedBetweenAsync_WithAnAllTimeRange_CountsEveryNonCancelledInvitation()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddDays(-400));
        AddInvitation(context, Base.AddDays(-30));
        AddInvitation(context, Base);
        AddInvitation(context, Base.AddDays(-5), cancelled: true);
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);
        var allTimeStart = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        var count = await repository.CountCreatedBetweenAsync(allTimeStart, Base.AddDays(1));

        count.Should().Be(3);
    }

    [Fact]
    public async Task CountCreatedBetweenAsync_WhenNothingInRange_ReturnsZero()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddDays(-30));
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);

        var count = await repository.CountCreatedBetweenAsync(Base.AddDays(-1), Base);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetCreatedCountsByBucketAsync_GroupsByDayExcludingCancelledAndOutOfRange()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddDays(-1).AddHours(-3));
        AddInvitation(context, Base.AddDays(-1).AddHours(2));
        AddInvitation(context, Base);
        AddInvitation(context, Base.AddDays(-1), cancelled: true);
        AddInvitation(context, Base.AddDays(-30));
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);

        var buckets = await repository.GetCreatedCountsByBucketAsync(Base.AddDays(-2), Base, groupByHour: false);

        buckets.Should().HaveCount(2);
        buckets[Base.AddDays(-1).Date].Should().Be(2);
        buckets[Base.Date].Should().Be(1);
    }

    [Fact]
    public async Task GetCreatedCountsByBucketAsync_GroupsByHour()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddHours(-2).AddMinutes(5));
        AddInvitation(context, Base.AddHours(-2).AddMinutes(55));
        AddInvitation(context, Base.AddHours(-1));
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);

        var buckets = await repository.GetCreatedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: true);

        buckets.Should().HaveCount(2);
        buckets[Base.AddHours(-2)].Should().Be(2);
        buckets[Base.AddHours(-1)].Should().Be(1);
    }

    [Fact]
    public async Task GetCreatedCountsByBucketAsync_WhenNothingInRange_ReturnsEmpty()
    {
        await using var context = NewContext();
        AddInvitation(context, Base.AddDays(-30));
        await context.SaveChangesAsync();
        var repository = new InterviewInvitationRepository(context);

        var buckets = await repository.GetCreatedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: false);

        buckets.Should().BeEmpty();
    }
}

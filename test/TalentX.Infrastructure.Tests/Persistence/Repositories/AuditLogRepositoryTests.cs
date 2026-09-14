using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class AuditLogRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_PersistsAuditLogToDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);
        var auditLog = new AuditLog("user@talentx.com", "Insert · Job", isSystem: false);

        // Act
        await repository.AddAsync(auditLog, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var savedAuditLog = await context.AuditLogs.FirstOrDefaultAsync(x => x.Id == auditLog.Id);
        savedAuditLog.Should().NotBeNull();
        savedAuditLog!.Actor.Should().Be("user@talentx.com");
        savedAuditLog.Action.Should().Be("Insert · Job");
        savedAuditLog.IsSystem.Should().BeFalse();
        savedAuditLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddRangeAsync_PersistsMultipleAuditLogsToDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);
        var auditLogs = new List<AuditLog>
        {
            new("admin@talentx.com", "Login Success", isSystem: false),
            new("system", "Update · ApplicationUser", isSystem: true),
            new("unknown@talentx.com", "Login Failed", isSystem: false)
        };

        // Act
        await repository.AddRangeAsync(auditLogs, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var count = await context.AuditLogs.CountAsync();
        count.Should().Be(3);

        var retrievedLogs = await context.AuditLogs.OrderBy(x => x.CreatedAt).ToListAsync();
        retrievedLogs.Should().HaveCount(3);
        retrievedLogs.Select(x => x.Action).Should().Contain(new[]
        {
            "Login Success",
            "Update · ApplicationUser",
            "Login Failed"
        });
    }

    [Fact]
    public async Task AddAsync_WithCancellationToken_HonorsCancellationToken()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);
        var auditLog = new AuditLog("user@talentx.com", "Delete · Job", isSystem: false);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await repository.AddAsync(auditLog, cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddRangeAsync_WithEmptyList_DoesNotThrowAndPersistsNothing()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);

        // Act
        await repository.AddRangeAsync(Enumerable.Empty<AuditLog>(), CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert
        var count = await context.AuditLogs.CountAsync();
        count.Should().Be(0);
    }

    private static void SetCreatedAt(ApplicationDbContext context, AuditLog auditLog, DateTime createdAt)
    {
        context.Entry(auditLog).Property(x => x.CreatedAt).CurrentValue = createdAt;
    }

    [Fact]
    public async Task GetPagedAuditLogsAsync_WithSearchQuery_FiltersByActorOrActionCaseInsensitive()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);

        var log1 = new AuditLog("admin@talentx.com", "Insert · Job", isSystem: false);
        var log2 = new AuditLog("system", "Update · ApplicationUser", isSystem: true);
        var log3 = new AuditLog("user@talentx.com", "Delete · Job", isSystem: false);

        await context.AuditLogs.AddRangeAsync(log1, log2, log3);
        await context.SaveChangesAsync();

        // Act - Search by actor
        var (actorItems, actorCount) = await repository.GetPagedAuditLogsAsync(
            searchQuery: "ADMIN",
            startDate: null,
            endDate: null,
            page: 1,
            pageSize: 10);

        // Act - Search by action
        var (actionItems, actionCount) = await repository.GetPagedAuditLogsAsync(
            searchQuery: "job",
            startDate: null,
            endDate: null,
            page: 1,
            pageSize: 10);

        // Assert
        actorCount.Should().Be(1);
        actorItems.Should().ContainSingle(x => x.Actor == "admin@talentx.com");

        actionCount.Should().Be(2);
        actionItems.Select(x => x.Action).Should().BeEquivalentTo(new[] { "Insert · Job", "Delete · Job" });
    }

    [Fact]
    public async Task GetPagedAuditLogsAsync_WithDateRange_FiltersByStartDateAndEndDate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);

        var baseTime = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var earlyLog = new AuditLog("early@talentx.com", "Action 1");
        var midLog = new AuditLog("mid@talentx.com", "Action 2");
        var lateLog = new AuditLog("late@talentx.com", "Action 3");

        await context.AuditLogs.AddRangeAsync(earlyLog, midLog, lateLog);
        await context.SaveChangesAsync();

        SetCreatedAt(context, earlyLog, baseTime.AddDays(-5));
        SetCreatedAt(context, midLog, baseTime);
        SetCreatedAt(context, lateLog, baseTime.AddDays(5));
        await context.SaveChangesAsync();

        // Act - Between StartDate and EndDate
        var (midItems, midCount) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: baseTime.AddDays(-1),
            endDate: baseTime.AddDays(1),
            page: 1,
            pageSize: 10);

        // Act - StartDate only
        var (sinceMidItems, sinceMidCount) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: baseTime,
            endDate: null,
            page: 1,
            pageSize: 10);

        // Act - EndDate only
        var (untilMidItems, untilMidCount) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: null,
            endDate: baseTime,
            page: 1,
            pageSize: 10);

        // Assert
        midCount.Should().Be(1);
        midItems.Should().ContainSingle(x => x.Actor == "mid@talentx.com");

        sinceMidCount.Should().Be(2);
        sinceMidItems.Select(x => x.Actor).Should().BeEquivalentTo(new[] { "mid@talentx.com", "late@talentx.com" });

        untilMidCount.Should().Be(2);
        untilMidItems.Select(x => x.Actor).Should().BeEquivalentTo(new[] { "early@talentx.com", "mid@talentx.com" });
    }

    [Fact]
    public async Task GetPagedAuditLogsAsync_WithPaginationAndOrdering_ReturnsNewestFirstPaged()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);

        var baseTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var logs = new List<AuditLog>();
        for (var i = 1; i <= 5; i++)
        {
            logs.Add(new AuditLog($"user{i}@talentx.com", $"Action {i}"));
        }

        await context.AuditLogs.AddRangeAsync(logs);
        await context.SaveChangesAsync();

        for (var i = 0; i < logs.Count; i++)
        {
            SetCreatedAt(context, logs[i], baseTime.AddHours(i));
        }
        await context.SaveChangesAsync();

        // Act - Page 1 with pageSize 2
        var (page1Items, page1Total) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: null,
            endDate: null,
            page: 1,
            pageSize: 2);

        // Act - Page 2 with pageSize 2
        var (page2Items, page2Total) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: null,
            endDate: null,
            page: 2,
            pageSize: 2);

        // Act - Page 3 with pageSize 2
        var (page3Items, page3Total) = await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: null,
            endDate: null,
            page: 3,
            pageSize: 2);

        // Assert
        page1Total.Should().Be(5);
        page1Items.Should().HaveCount(2);
        page1Items[0].Actor.Should().Be("user5@talentx.com"); // Newest first
        page1Items[1].Actor.Should().Be("user4@talentx.com");

        page2Total.Should().Be(5);
        page2Items.Should().HaveCount(2);
        page2Items[0].Actor.Should().Be("user3@talentx.com");
        page2Items[1].Actor.Should().Be("user2@talentx.com");

        page3Total.Should().Be(5);
        page3Items.Should().HaveCount(1);
        page3Items[0].Actor.Should().Be("user1@talentx.com");
    }

    [Fact]
    public async Task GetPagedAuditLogsAsync_WithCancellationToken_HonorsCancellationToken()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new AuditLogRepository(context);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await repository.GetPagedAuditLogsAsync(
            searchQuery: null,
            startDate: null,
            endDate: null,
            page: 1,
            pageSize: 10,
            cancellationToken: cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

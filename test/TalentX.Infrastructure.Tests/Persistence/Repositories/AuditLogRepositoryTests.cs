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
}

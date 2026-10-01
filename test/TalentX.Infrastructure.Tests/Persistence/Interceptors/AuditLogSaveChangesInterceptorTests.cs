using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Interceptors;

public sealed class AuditLogSaveChangesInterceptorTests
{
    private static ApplicationDbContext CreateContext(string? userEmail = null, string? ipAddress = null)
    {
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.SetupGet(service => service.Email).Returns(userEmail);
        currentUserServiceMock.SetupGet(service => service.IpAddress).Returns(ipAddress);

        var interceptor = new AuditLogSaveChangesInterceptor(currentUserServiceMock.Object);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job CreateSampleJob()
    {
        return Job.Create(
            "employer-1",
            "Software Engineer",
            "Acme Corporation",
            "Job description text.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityInserted_AddsInsertAuditLog()
    {
        // Arrange
        await using var context = CreateContext("author@talentx.com");
        var job = CreateSampleJob();

        // Act
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Action.Should().Be("Insert · Job");
        auditLog.Actor.Should().Be("author@talentx.com");
        auditLog.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityUpdated_AddsUpdateAuditLog()
    {
        // Arrange
        await using var context = CreateContext("editor@talentx.com");
        var job = CreateSampleJob();
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Clear previous insert audit log to isolate update assertion
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();

        // Act
        job.SetDetails(
            "Senior Software Engineer",
            job.CompanyName,
            job.Description,
            job.EmploymentType,
            job.ExperienceLevel,
            job.Location,
            job.WorkArrangement,
            job.RequiredSkills,
            job.Vacancies,
            job.ExpireAt);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Action.Should().Be("Update · Job");
        auditLog.Actor.Should().Be("editor@talentx.com");
        auditLog.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityDeleted_AddsDeleteAuditLog()
    {
        // Arrange
        await using var context = CreateContext("admin@talentx.com");
        var job = CreateSampleJob();
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Clear previous insert audit log to isolate delete assertion
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();

        // Act
        context.Jobs.Remove(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Action.Should().Be("Delete · Job");
        auditLog.Actor.Should().Be("admin@talentx.com");
        auditLog.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenAuditLogEntityAdded_DoesNotTriggerRecursiveAuditLog()
    {
        // Arrange
        await using var context = CreateContext("system@talentx.com");
        var directAuditLog = new AuditLog("system@talentx.com", "Manual Operation", isSystem: false);

        // Act
        await context.AuditLogs.AddAsync(directAuditLog);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();
        auditLogs.Single().Action.Should().Be("Manual Operation");
        auditLogs.Should().NotContain(log => log.Action.Contains("AuditLog"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SavingChangesAsync_WhenUnauthenticatedOrEmptyEmail_SetsActorToSystemAndIsSystemToTrue(string? email)
    {
        // Arrange
        await using var context = CreateContext(email);
        var job = CreateSampleJob();

        // Act
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Actor.Should().Be("System");
        auditLog.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenAuthenticatedEmail_SetsActorToEmailAndIsSystemToFalse()
    {
        // Arrange
        const string userEmail = "recruiter@talentx.com";
        await using var context = CreateContext(userEmail);
        var job = CreateSampleJob();

        // Act
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Actor.Should().Be(userEmail);
        auditLog.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void SavingChanges_SynchronousInvocation_AddsInsertAuditLog()
    {
        // Arrange
        using var context = CreateContext("sync-user@talentx.com");
        var job = CreateSampleJob();

        // Act
        context.Jobs.Add(job);
        context.SaveChanges();

        // Assert
        var auditLogs = context.AuditLogs.ToList();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.Action.Should().Be("Insert · Job");
        auditLog.Actor.Should().Be("sync-user@talentx.com");
        auditLog.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenIpAddressAvailable_CapturesIpAddressInAuditLog()
    {
        // Arrange
        const string expectedIp = "203.0.113.195";
        await using var context = CreateContext("author@talentx.com", expectedIp);
        var job = CreateSampleJob();

        // Act
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.IpAddress.Should().Be(expectedIp);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenIpAddressNull_AuditLogInDatabaseHasNullIpAddress()
    {
        // Arrange
        await using var context = CreateContext("author@talentx.com", ipAddress: null);
        var job = CreateSampleJob();

        // Act
        await context.Jobs.AddAsync(job);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle();

        var auditLog = auditLogs.Single();
        auditLog.IpAddress.Should().BeNull();
    }
}

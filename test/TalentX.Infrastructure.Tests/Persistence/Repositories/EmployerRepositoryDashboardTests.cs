using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class EmployerRepositoryDashboardTests
{
    private static readonly DateTime Base = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Employer AddEmployer(
        ApplicationDbContext context,
        string company,
        DateTime createdAt,
        EmployerVerificationStatus status = EmployerVerificationStatus.PendingVerification)
    {
        var employer = new Employer(Guid.NewGuid().ToString(), $"{company} Owner", $"{company.ToLowerInvariant()}@example.com", company);
        if (status == EmployerVerificationStatus.Verified)
        {
            employer.MarkAsVerified();
        }
        else if (status == EmployerVerificationStatus.Rejected)
        {
            employer.MarkAsRejected("Documents could not be verified.");
        }

        context.Employers.Add(employer);
        context.Entry(employer).Property(e => e.CreatedAt).CurrentValue = createdAt;
        return employer;
    }

    [Fact]
    public async Task GetVerificationStatusCountsCreatedBetweenAsync_GroupsByStatusWithinRange()
    {
        await using var context = NewContext();
        AddEmployer(context, "VerifiedOne", Base.AddHours(-1), EmployerVerificationStatus.Verified);
        AddEmployer(context, "VerifiedTwo", Base.AddHours(-2), EmployerVerificationStatus.Verified);
        AddEmployer(context, "PendingOne", Base.AddHours(-3));
        AddEmployer(context, "RejectedOne", Base.AddHours(-4), EmployerVerificationStatus.Rejected);
        AddEmployer(context, "OutOfRange", Base.AddDays(-20), EmployerVerificationStatus.Verified);
        await context.SaveChangesAsync();
        var repository = new EmployerRepository(context);

        var counts = await repository.GetVerificationStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

        counts[EmployerVerificationStatus.Verified].Should().Be(2);
        counts[EmployerVerificationStatus.PendingVerification].Should().Be(1);
        counts[EmployerVerificationStatus.Rejected].Should().Be(1);
    }

    [Fact]
    public async Task GetVerificationStatusCountsCreatedBetweenAsync_OmitsStatusesWithNoEmployers()
    {
        await using var context = NewContext();
        AddEmployer(context, "OnlyPending", Base.AddHours(-1));
        await context.SaveChangesAsync();
        var repository = new EmployerRepository(context);

        var counts = await repository.GetVerificationStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

        counts.Should().ContainSingle().Which.Key.Should().Be(EmployerVerificationStatus.PendingVerification);
    }

    [Fact]
    public async Task GetOldestPendingCreatedAtAsync_IgnoresTheRangeAndNonPendingEmployers()
    {
        await using var context = NewContext();
        AddEmployer(context, "OldVerified", Base.AddDays(-90), EmployerVerificationStatus.Verified);
        AddEmployer(context, "OldestPending", Base.AddDays(-40));
        AddEmployer(context, "NewerPending", Base.AddDays(-2));
        await context.SaveChangesAsync();
        var repository = new EmployerRepository(context);

        var oldest = await repository.GetOldestPendingCreatedAtAsync();

        oldest.Should().Be(Base.AddDays(-40));
    }

    [Fact]
    public async Task GetOldestPendingCreatedAtAsync_WhenNothingIsPending_ReturnsNull()
    {
        await using var context = NewContext();
        AddEmployer(context, "Verified", Base.AddDays(-5), EmployerVerificationStatus.Verified);
        await context.SaveChangesAsync();
        var repository = new EmployerRepository(context);

        var oldest = await repository.GetOldestPendingCreatedAtAsync();

        oldest.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentCreatedBetweenAsync_ReturnsNewestFirstAndHonoursTake()
    {
        await using var context = NewContext();
        AddEmployer(context, "Oldest", Base.AddHours(-3));
        AddEmployer(context, "Newest", Base.AddHours(-1));
        AddEmployer(context, "Middle", Base.AddHours(-2));
        AddEmployer(context, "OutOfRange", Base.AddDays(-10));
        await context.SaveChangesAsync();
        var repository = new EmployerRepository(context);

        var recent = await repository.GetRecentCreatedBetweenAsync(Base.AddDays(-1), Base, take: 2);

        recent.Select(e => e.CompanyName).Should().Equal("Newest", "Middle");
    }
}

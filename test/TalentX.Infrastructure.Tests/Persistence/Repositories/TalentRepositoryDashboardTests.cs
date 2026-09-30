using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class TalentRepositoryDashboardTests
{
    private static readonly DateTime Base = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Talent AddTalent(ApplicationDbContext context, string name, DateTime createdAt)
    {
        var talent = new Talent(Guid.NewGuid().ToString(), name, $"{name.ToLowerInvariant()}@example.com");
        context.Talents.Add(talent);
        context.Entry(talent).Property(t => t.CreatedAt).CurrentValue = createdAt;
        return talent;
    }

    [Fact]
    public async Task CountCreatedBetweenAsync_CountsOnlyTalentsInsideTheInclusiveRange()
    {
        await using var context = NewContext();
        AddTalent(context, "Before", Base.AddDays(-2));
        AddTalent(context, "AtStart", Base.AddDays(-1));
        AddTalent(context, "Middle", Base);
        AddTalent(context, "AtEnd", Base.AddDays(1));
        AddTalent(context, "After", Base.AddDays(2));
        await context.SaveChangesAsync();
        var repository = new TalentRepository(context);

        var count = await repository.CountCreatedBetweenAsync(Base.AddDays(-1), Base.AddDays(1));

        count.Should().Be(3);
    }

    [Fact]
    public async Task CountCreatedBetweenAsync_WhenNoTalentInRange_ReturnsZero()
    {
        await using var context = NewContext();
        AddTalent(context, "Old", Base.AddDays(-30));
        await context.SaveChangesAsync();
        var repository = new TalentRepository(context);

        var count = await repository.CountCreatedBetweenAsync(Base.AddDays(-1), Base.AddDays(1));

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetRecentCreatedBetweenAsync_ReturnsNewestFirstAndHonoursTake()
    {
        await using var context = NewContext();
        AddTalent(context, "Oldest", Base.AddHours(-3));
        AddTalent(context, "Newest", Base.AddHours(-1));
        AddTalent(context, "Middle", Base.AddHours(-2));
        AddTalent(context, "OutOfRange", Base.AddDays(-10));
        await context.SaveChangesAsync();
        var repository = new TalentRepository(context);

        var recent = await repository.GetRecentCreatedBetweenAsync(Base.AddDays(-1), Base, take: 2);

        recent.Select(t => t.Name).Should().Equal("Newest", "Middle");
    }
}

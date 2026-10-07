using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobTalentMatchRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByJobIdAsync_ReturnsMatchesOrderedByScoreDescendingThenTalentUserId()
    {
        await using var context = CreateInMemoryContext();
        var repository = new JobTalentMatchRepository(context);
        var jobId = Guid.NewGuid();

        await repository.AddAsync(new JobTalentMatch(jobId, "talent-mid", 70m, "mid"));
        await repository.AddAsync(new JobTalentMatch(jobId, "talent-zed", 90m, "top-z"));
        await repository.AddAsync(new JobTalentMatch(jobId, "talent-abc", 90m, "top-a"));
        await repository.AddAsync(new JobTalentMatch(jobId, "talent-low", 40m, "low"));
        await context.SaveChangesAsync();

        var matches = await repository.GetByJobIdAsync(jobId);

        matches.Select(m => m.TalentUserId)
            .Should().ContainInOrder("talent-abc", "talent-zed", "talent-mid", "talent-low");
    }

    [Fact]
    public async Task GetByJobIdAsync_ExcludesMatchesFromOtherJobs()
    {
        await using var context = CreateInMemoryContext();
        var repository = new JobTalentMatchRepository(context);
        var jobId = Guid.NewGuid();

        await repository.AddAsync(new JobTalentMatch(jobId, "talent-1", 80m));
        await repository.AddAsync(new JobTalentMatch(Guid.NewGuid(), "talent-2", 99m));
        await context.SaveChangesAsync();

        var matches = await repository.GetByJobIdAsync(jobId);

        matches.Should().ContainSingle();
        matches[0].TalentUserId.Should().Be("talent-1");
    }

    [Fact]
    public async Task AddAsync_PersistsInputHash()
    {
        await using var context = CreateInMemoryContext();
        var repository = new JobTalentMatchRepository(context);
        var jobId = Guid.NewGuid();

        await repository.AddAsync(new JobTalentMatch(jobId, "talent-1", 85m, "why", "hash-123"));
        await context.SaveChangesAsync();

        var reloaded = await context.JobTalentMatches.AsNoTracking().SingleAsync();

        reloaded.InputHash.Should().Be("hash-123");
    }

    [Fact]
    public async Task Update_PersistsNewScoreAndInputHash()
    {
        await using var context = CreateInMemoryContext();
        var repository = new JobTalentMatchRepository(context);
        var jobId = Guid.NewGuid();
        var match = new JobTalentMatch(jobId, "talent-1", 85m, "old", "hash-old");
        await repository.AddAsync(match);
        await context.SaveChangesAsync();

        match.UpdateScore(95m, "new", "hash-new");
        repository.Update(match);
        await context.SaveChangesAsync();

        var reloaded = await context.JobTalentMatches.AsNoTracking().SingleAsync();
        reloaded.Score.Should().Be(95m);
        reloaded.InputHash.Should().Be("hash-new");
    }

    [Fact]
    public async Task GetByJobIdAndTalentUserIdAsync_ReturnsMatchingRow()
    {
        await using var context = CreateInMemoryContext();
        var repository = new JobTalentMatchRepository(context);
        var jobId = Guid.NewGuid();

        await repository.AddAsync(new JobTalentMatch(jobId, "talent-1", 85m, "why", "hash-1"));
        await repository.AddAsync(new JobTalentMatch(jobId, "talent-2", 65m, "why", "hash-2"));
        await context.SaveChangesAsync();

        var match = await repository.GetByJobIdAndTalentUserIdAsync(jobId, "talent-2");

        match.Should().NotBeNull();
        match!.Score.Should().Be(65m);
        match.InputHash.Should().Be("hash-2");
    }
}

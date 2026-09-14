using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class TalentNotificationPreferenceRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetByTalentIdAsync_PersistsAndRetrievesPreference()
    {
        await using var context = NewContext();
        var repository = new TalentNotificationPreferenceRepository(context);

        var preference = TalentNotificationPreference.CreateDefault("talent-1");
        preference.Update(false);

        await repository.AddAsync(preference);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByTalentIdAsync("talent-1");

        retrieved.Should().NotBeNull();
        retrieved!.TalentId.Should().Be("talent-1");
        retrieved.JobAlertsEmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTalentIdAsync_WhenNotExists_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new TalentNotificationPreferenceRepository(context);

        var retrieved = await repository.GetByTalentIdAsync("non-existent-talent");

        retrieved.Should().BeNull();
    }
}

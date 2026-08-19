using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class UnitOfWorkTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToTheUnderlyingContext()
    {
        await using var context = NewContext();
        var unitOfWork = new UnitOfWork(context);
        var job = Job.CreateDraft(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Remote,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );
        context.Jobs.Add(job);

        var affectedRows = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        affectedRows.Should().Be(1);
        (await context.Jobs.FindAsync(job.Id)).Should().NotBeNull();
    }
}

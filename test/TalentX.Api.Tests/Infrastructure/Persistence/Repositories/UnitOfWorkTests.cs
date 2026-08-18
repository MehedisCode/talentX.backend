using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;

namespace TalentX.Api.Tests.Infrastructure.Persistence.Repositories;

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
    public void Constructor_ExposesAJobRepository()
    {
        using var context = NewContext();

        var unitOfWork = new UnitOfWork(context);

        unitOfWork.Jobs.Should().NotBeNull().And.BeAssignableTo<IJobRepository>();
    }

    [Fact]
    public void Constructor_ExposesAUserRepository()
    {
        using var context = NewContext();

        var unitOfWork = new UnitOfWork(context);

        unitOfWork.Users.Should().NotBeNull().And.BeAssignableTo<IUserRepository>();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToTheUnderlyingContext()
    {
        await using var context = NewContext();
        var unitOfWork = new UnitOfWork(context);
        var job = TalentX.Domain.Entities.Job.CreateDraft(
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            TalentX.Domain.Enums.EmploymentType.FullTime,
            TalentX.Domain.Enums.ExperienceLevel.MidLevel,
            "Dhaka",
            TalentX.Domain.Enums.WorkArrangement.Remote,
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

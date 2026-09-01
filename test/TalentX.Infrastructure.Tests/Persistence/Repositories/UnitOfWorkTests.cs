using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
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
    public void Constructor_ExposesAJobRepository()
    {
        using var context = NewContext();

        var unitOfWork = new UnitOfWork(context);

        unitOfWork.Jobs.Should().NotBeNull().And.BeAssignableTo<IJobRepository>();
        unitOfWork.JobApplications.Should().NotBeNull().And.BeAssignableTo<IJobApplicationRepository>();
        unitOfWork.Users.Should().NotBeNull().And.BeAssignableTo<IUserRepository>();
        unitOfWork.Employers.Should().NotBeNull().And.BeAssignableTo<IEmployerRepository>();
        unitOfWork.Talents.Should().NotBeNull().And.BeAssignableTo<ITalentRepository>();
        unitOfWork.Resumes.Should().NotBeNull().And.BeAssignableTo<IResumeRepository>();
        unitOfWork.WorkExperiences.Should().NotBeNull().And.BeAssignableTo<IWorkExperienceRepository>();
        unitOfWork.Educations.Should().NotBeNull().And.BeAssignableTo<IEducationRepository>();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToTheUnderlyingContext()
    {
        await using var context = NewContext();
        var unitOfWork = new UnitOfWork(context);
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
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

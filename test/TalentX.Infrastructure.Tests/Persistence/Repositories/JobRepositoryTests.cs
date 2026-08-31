using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class JobRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job MakePublishedJob(
        string title = "Backend Engineer",
        string company = "Acme Corp",
        string description = "Build and maintain backend services.",
        string location = "Dhaka",
        EmploymentType empType = EmploymentType.FullTime,
        ExperienceLevel expLevel = ExperienceLevel.MidLevel,
        WorkArrangement arrangement = WorkArrangement.RemoteButAccessible,
        int daysUntilExpiry = 30)
    {
        return Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.RemoteButAccessible,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );
    }

    // ---------- Existing CRUD tests (unchanged) ----------

    [Fact]
    public async Task GetByIdAsync_WhenJobExists_ReturnsIt()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakePublishedJob();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(job.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenJobDoesNotExist_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_TracksTheJobForInsertion()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakePublishedJob();

        await repository.AddAsync(job);
        await context.SaveChangesAsync();

        (await context.Jobs.FindAsync(job.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_DoesNotChangeTheAlreadyAssignedId()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakePublishedJob();
        var originalId = job.Id;
        originalId.Should().NotBe(Guid.Empty);

        await repository.AddAsync(job);

        job.Id.Should().Be(originalId);
    }

    [Fact]
    public void Update_MarksTheJobAsModified()
    {
        using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakePublishedJob();
        context.Jobs.Add(job);
        context.SaveChanges();
        context.Entry(job).State = EntityState.Detached;

        job.SetDetails(
            "Updated Title", job.CompanyName, job.Description, job.EmploymentType,
            job.ExperienceLevel, job.Location, job.WorkArrangement, job.RequiredSkills,
            job.Vacancies, job.ExpireAt
        );
        repository.Update(job);

        context.Entry(job).State.Should().Be(EntityState.Modified);
    }

    // ---------- Filter tests ----------

    [Fact]
    public async Task GetPublishedJobsAsync_ExcludesExpiredJobs()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        var active = MakePublishedJob("Active Job", daysUntilExpiry: 10);
        var expired = Job.Create("emp", "Expired Job", "Co", "Desc",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka",
            WorkArrangement.RemoteButAccessible, new List<string>(), 1, DateTime.UtcNow.AddDays(-1));
        expired.MarkAsPublished();

        context.Jobs.AddRange(active, expired);
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Active Job");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_ExcludesDraftJobs()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        var published = MakePublishedJob("Published Job");
        var draft = Job.Create("emp", "Draft Job", "Co", "Desc",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka",
            WorkArrangement.RemoteButAccessible, new List<string>(), 1, DateTime.UtcNow.AddDays(30));
        // draft.MarkAsPublished() intentionally omitted

        context.Jobs.AddRange(published, draft);
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Published Job");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_KeywordFilter_MatchesTitle()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob("React Developer"),
            MakePublishedJob("Backend Engineer"));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, keyword: "react");

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("React Developer");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_KeywordFilter_MatchesCompanyName()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob("Dev", "Northwind Labs"),
            MakePublishedJob("Dev", "Acme Corp"));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, keyword: "northwind");

        result.Items.Should().HaveCount(1);
        result.Items[0].CompanyName.Should().Be("Northwind Labs");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_KeywordFilter_IsCaseInsensitive()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.Add(MakePublishedJob("SENIOR BACKEND ENGINEER"));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, keyword: "senior backend");

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPublishedJobsAsync_KeywordFilter_ReturnsEmpty_WhenNoMatch()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.Add(MakePublishedJob("Frontend Developer"));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, keyword: "zxqwerty999");

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPublishedJobsAsync_LocationFilter_MatchesLocation()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob(location: "Dhaka"),
            MakePublishedJob(location: "Chittagong"));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, location: "Dhaka");

        result.Items.Should().HaveCount(1);
        result.Items[0].Location.Should().Be("Dhaka");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_EmploymentTypeFilter_ReturnsOnlyMatchingJobs()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob(empType: EmploymentType.FullTime),
            MakePublishedJob(empType: EmploymentType.Contract),
            MakePublishedJob(empType: EmploymentType.Internship));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(
            1, 20, CancellationToken.None, employmentType: EmploymentType.Contract);

        result.Items.Should().HaveCount(1);
        result.Items[0].EmploymentType.Should().Be(EmploymentType.Contract);
    }

    [Fact]
    public async Task GetPublishedJobsAsync_ExperienceLevelFilter_ReturnsOnlyMatchingJobs()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob(expLevel: ExperienceLevel.EntryLevel),
            MakePublishedJob(expLevel: ExperienceLevel.SeniorLevel));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(
            1, 20, CancellationToken.None, experienceLevel: ExperienceLevel.SeniorLevel);

        result.Items.Should().HaveCount(1);
        result.Items[0].ExperienceLevel.Should().Be(ExperienceLevel.SeniorLevel);
    }

    [Fact]
    public async Task GetPublishedJobsAsync_WorkArrangementFilter_ReturnsOnlyMatchingJobs()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        context.Jobs.AddRange(
            MakePublishedJob(arrangement: WorkArrangement.RemoteButAccessible),
            MakePublishedJob(arrangement: WorkArrangement.OnSite),
            MakePublishedJob(arrangement: WorkArrangement.Hybrid));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(
            1, 20, CancellationToken.None, workArrangement: WorkArrangement.RemoteButAccessible );

        result.Items.Should().HaveCount(1);
        result.Items[0].WorkArrangement.Should().Be(WorkArrangement.RemoteButAccessible);
    }

    [Fact]
    public async Task GetPublishedJobsAsync_CompoundFilters_ApplyAsAnd()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        // Match: FullTime + Senior + Hybrid
        context.Jobs.Add(MakePublishedJob("Match", empType: EmploymentType.FullTime,
            expLevel: ExperienceLevel.SeniorLevel, arrangement: WorkArrangement.Hybrid));
        // Miss: FullTime + Senior + Remote
        context.Jobs.Add(MakePublishedJob("Miss", empType: EmploymentType.FullTime,
            expLevel: ExperienceLevel.SeniorLevel, arrangement: WorkArrangement.RemoteButAccessible));
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(
            1, 20, CancellationToken.None,
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            workArrangement: WorkArrangement.Hybrid);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Match");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_Pagination_ReturnsCorrectSlice()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        for (int i = 0; i < 5; i++)
            context.Jobs.Add(MakePublishedJob($"Job {i}"));
        await context.SaveChangesAsync();

        var page1 = await repo.GetPublishedJobsAsync(1, 2, CancellationToken.None);
        var page2 = await repo.GetPublishedJobsAsync(2, 2, CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.Items[0].Title.Should().NotBe(page2.Items[0].Title);
    }
}

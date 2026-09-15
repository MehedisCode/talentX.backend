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
        var job = Job.Create(
            "employer-1",
            title,
            company,
            description,
            empType,
            expLevel,
            location,
            arrangement,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(daysUntilExpiry)
        );
        job.MarkAsPublished();
        return job;
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

    [Fact]
    public async Task GetPublishedJobsAsync_KeywordFilter_MatchesDescription()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        var job1 = MakePublishedJob("Lead Architect", description: "Deep expertise in Kubernetes and distributed systems.");
        var job2 = MakePublishedJob("Product Designer", description: "Design Figma wireframes and prototypes.");
        context.Jobs.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, keyword: "Kubernetes");

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Lead Architect");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_LocationFilter_MatchesCity()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        var job = MakePublishedJob("Software Engineer", location: "On-site");
        job.UpdateWizardDetails("Berlin", "Germany", null, null);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, location: "berlin");

        result.Items.Should().HaveCount(1);
        result.Items[0].City.Should().Be("Berlin");
    }

    [Fact]
    public async Task GetPublishedJobsAsync_LocationFilter_MatchesCountry()
    {
        await using var context = NewContext();
        var repo = new JobRepository(context);

        var job = MakePublishedJob("Software Engineer", location: "Remote");
        job.UpdateWizardDetails("Tokyo", "Japan", null, null);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repo.GetPublishedJobsAsync(1, 20, CancellationToken.None, location: "japan");

        result.Items.Should().HaveCount(1);
        result.Items[0].Country.Should().Be("Japan");
    }

    [Fact]
    public async Task GetEmployerStatsAsync_WhenJobsExist_ReturnsAccurateMetrics()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var activeJob = MakeCustomJob("employer-1", "Active Job", JobStatus.Published, daysUntilExpiry: 10);
        var expiredJob = MakeCustomJob("employer-1", "Expired Job", JobStatus.Published, daysUntilExpiry: -2);
        var draftJob = MakeCustomJob("employer-1", "Draft Job", JobStatus.Draft, daysUntilExpiry: 30);
        var closedJob = MakeCustomJob("employer-1", "Closed Job", JobStatus.Closed, daysUntilExpiry: 30);
        var softDeletedJob = MakeCustomJob("employer-1", "Deleted Job", JobStatus.Published, daysUntilExpiry: 10);
        softDeletedJob.SoftDelete();

        var otherEmployerJob = MakeCustomJob("employer-2", "Other Employer Job", JobStatus.Published, daysUntilExpiry: 10);

        context.Jobs.AddRange(activeJob, expiredJob, draftJob, closedJob, softDeletedJob, otherEmployerJob);
        await context.SaveChangesAsync();

        var stats = await repository.GetEmployerStatsAsync("employer-1");

        stats.TotalPostedJobs.Should().Be(4);
        stats.ActiveJobsCount.Should().Be(1);
        stats.DraftJobsCount.Should().Be(1);
        stats.ClosedJobsCount.Should().Be(1);
        stats.ExpiredJobsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEmployerStatsAsync_WhenEmployerHasNoJobs_ReturnsAllZeros()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var stats = await repository.GetEmployerStatsAsync("non-existent-employer");

        stats.TotalPostedJobs.Should().Be(0);
        stats.ActiveJobsCount.Should().Be(0);
        stats.DraftJobsCount.Should().Be(0);
        stats.ClosedJobsCount.Should().Be(0);
        stats.ExpiredJobsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetEmployerStatsAsync_ExcludesSoftDeletedJobs()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var softDeletedJob = MakeCustomJob("employer-1", "Deleted Job", JobStatus.Published, daysUntilExpiry: 10);
        softDeletedJob.SoftDelete();

        context.Jobs.Add(softDeletedJob);
        await context.SaveChangesAsync();

        var stats = await repository.GetEmployerStatsAsync("employer-1");

        stats.TotalPostedJobs.Should().Be(0);
        stats.ActiveJobsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRecentJobsByEmployerIdAsync_WhenJobsExist_ReturnsTopJobsOrderedByCreatedAtWithApplicantCounts()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var firstJob = MakeCustomJob("employer-1", "First Job", JobStatus.Published, daysUntilExpiry: 10);
        var secondJob = MakeCustomJob("employer-1", "Second Job", JobStatus.Draft, daysUntilExpiry: 10);
        var thirdJob = MakeCustomJob("employer-1", "Third Job", JobStatus.Closed, daysUntilExpiry: 10);
        var fourthJob = MakeCustomJob("employer-1", "Fourth Job", JobStatus.Published, daysUntilExpiry: 10);
        var deletedJob = MakeCustomJob("employer-1", "Deleted Job", JobStatus.Published, daysUntilExpiry: 10);
        deletedJob.SoftDelete();
        var otherEmployerJob = MakeCustomJob("employer-2", "Other Job", JobStatus.Published, daysUntilExpiry: 10);

        context.Jobs.AddRange(firstJob, secondJob, thirdJob, fourthJob, deletedJob, otherEmployerJob);
        await context.SaveChangesAsync();

        context.Entry(firstJob).Property(job => job.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-4);
        context.Entry(secondJob).Property(job => job.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-3);
        context.Entry(thirdJob).Property(job => job.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-2);
        context.Entry(fourthJob).Property(job => job.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-1);
        await context.SaveChangesAsync();

        var applicationOne = MakeJobApplication(fourthJob.Id, "talent-1");
        var applicationTwo = MakeJobApplication(fourthJob.Id, "talent-2");
        var applicationThree = MakeJobApplication(thirdJob.Id, "talent-3");
        context.JobApplications.AddRange(applicationOne, applicationTwo, applicationThree);
        await context.SaveChangesAsync();

        var recentJobs = await repository.GetRecentJobsByEmployerIdAsync("employer-1", count: 2);

        recentJobs.Should().HaveCount(2);
        recentJobs[0].Id.Should().Be(fourthJob.Id);
        recentJobs[0].ApplicantCount.Should().Be(2);
        recentJobs[0].Status.Should().Be("Published");

        recentJobs[1].Id.Should().Be(thirdJob.Id);
        recentJobs[1].ApplicantCount.Should().Be(1);
        recentJobs[1].Status.Should().Be("Closed");
    }

    [Fact]
    public async Task GetRecentJobsByEmployerIdAsync_WhenJobIsExpired_ReportsExpiredStatus()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var expiredJob = MakeCustomJob("employer-1", "Expired Job", JobStatus.Published, daysUntilExpiry: -5);
        context.Jobs.Add(expiredJob);
        await context.SaveChangesAsync();

        var recentJobs = await repository.GetRecentJobsByEmployerIdAsync("employer-1", count: 5);

        recentJobs.Should().HaveCount(1);
        recentJobs[0].Status.Should().Be("Expired");
    }

    [Fact]
    public async Task GetRecentJobsByEmployerIdAsync_WhenNoJobs_ReturnsEmptyList()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var recentJobs = await repository.GetRecentJobsByEmployerIdAsync("employer-without-jobs");

        recentJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_Pagination_ReturnsCorrectPageSliceAndTotal()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        for (var index = 0; index < 5; index++)
        {
            var job = MakeCustomJob("employer-1", $"Job {index}");
            context.Jobs.Add(job);
        }
        await context.SaveChangesAsync();

        var pageOne = await repository.GetEmployerJobsPaginatedAsync("employer-1", null, null, page: 1, pageSize: 2);
        var pageThree = await repository.GetEmployerJobsPaginatedAsync("employer-1", null, null, page: 3, pageSize: 2);

        pageOne.Items.Should().HaveCount(2);
        pageOne.TotalCount.Should().Be(5);
        pageOne.Page.Should().Be(1);
        pageOne.PageSize.Should().Be(2);

        pageThree.Items.Should().HaveCount(1);
        pageThree.TotalCount.Should().Be(5);
        pageThree.Page.Should().Be(3);
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var draftJob = MakeCustomJob("employer-1", "Draft Job", JobStatus.Draft);
        var publishedJob = MakeCustomJob("employer-1", "Published Job", JobStatus.Published);
        var closedJob = MakeCustomJob("employer-1", "Closed Job", JobStatus.Closed);

        context.Jobs.AddRange(draftJob, publishedJob, closedJob);
        await context.SaveChangesAsync();

        var draftResult = await repository.GetEmployerJobsPaginatedAsync("employer-1", JobStatus.Draft, null, page: 1, pageSize: 10);
        var closedResult = await repository.GetEmployerJobsPaginatedAsync("employer-1", JobStatus.Closed, null, page: 1, pageSize: 10);

        draftResult.Items.Should().HaveCount(1);
        draftResult.Items[0].Title.Should().Be("Draft Job");

        closedResult.Items.Should().HaveCount(1);
        closedResult.Items[0].Title.Should().Be("Closed Job");
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_ExpirationFilters_FiltersCorrectly()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var activePublishedJob = MakeCustomJob("employer-1", "Active Published", JobStatus.Published, daysUntilExpiry: 10);
        var expiredPublishedJob = MakeCustomJob("employer-1", "Expired Published", JobStatus.Published, daysUntilExpiry: -5);
        var draftJob = MakeCustomJob("employer-1", "Draft Job", JobStatus.Draft, daysUntilExpiry: 10);

        context.Jobs.AddRange(activePublishedJob, expiredPublishedJob, draftJob);
        await context.SaveChangesAsync();

        var publishedActiveResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1", JobStatus.Published, isExpired: false, page: 1, pageSize: 10);
        var publishedExpiredResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1", JobStatus.Published, isExpired: true, page: 1, pageSize: 10);
        var anyExpiredResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1", status: null, isExpired: true, page: 1, pageSize: 10);
        var anyNotExpiredResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1", status: null, isExpired: false, page: 1, pageSize: 10);

        publishedActiveResult.Items.Should().HaveCount(1);
        publishedActiveResult.Items[0].Title.Should().Be("Active Published");
        publishedActiveResult.Items[0].Status.Should().Be("Published");

        publishedExpiredResult.Items.Should().HaveCount(1);
        publishedExpiredResult.Items[0].Title.Should().Be("Expired Published");
        publishedExpiredResult.Items[0].Status.Should().Be("Expired");

        anyExpiredResult.Items.Should().HaveCount(1);
        anyExpiredResult.Items[0].Title.Should().Be("Expired Published");

        anyNotExpiredResult.Items.Should().HaveCount(2);
        anyNotExpiredResult.Items.Select(item => item.Title).Should().Contain(["Active Published", "Draft Job"]);
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_ExcludesSoftDeletedJobsAndOtherEmployers()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var validJob = MakeCustomJob("employer-1", "Valid Job");
        var softDeletedJob = MakeCustomJob("employer-1", "Deleted Job");
        softDeletedJob.SoftDelete();
        var otherEmployerJob = MakeCustomJob("employer-2", "Other Employer Job");

        context.Jobs.AddRange(validJob, softDeletedJob, otherEmployerJob);
        await context.SaveChangesAsync();

        var result = await repository.GetEmployerJobsPaginatedAsync("employer-1", null, null, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Valid Job");
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_ComputesDisplayStatusAndApplicantCount()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var job = MakeCustomJob("employer-1", "Engineering Manager", JobStatus.Published, daysUntilExpiry: 15);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var application = MakeJobApplication(job.Id, "talent-1");
        context.JobApplications.Add(application);
        await context.SaveChangesAsync();

        var result = await repository.GetEmployerJobsPaginatedAsync("employer-1", null, null, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].ApplicantCount.Should().Be(1);
        result.Items[0].Status.Should().Be("Published");
        result.Items[0].CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_WhenSearchTermProvided_FiltersByTitleAndCompanyNameCaseInsensitively()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);

        var firstJob = MakeEmployerJob("employer-1", "Senior AI Engineer", "TechNova Corp");
        var secondJob = MakeEmployerJob("employer-1", "Junior Frontend Developer", "Senior Staffing Group");
        var thirdJob = MakeEmployerJob("employer-1", "DevOps Engineer", "Cloud Infrastructure LLC");
        var fourthJob = MakeEmployerJob("employer-1", "Product Manager", "Innovate Labs");

        context.Jobs.AddRange(firstJob, secondJob, thirdJob, fourthJob);
        await context.SaveChangesAsync();

        var titleAndCompanySearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "senior");

        titleAndCompanySearchResult.TotalCount.Should().Be(2);
        titleAndCompanySearchResult.Items.Should().HaveCount(2);
        titleAndCompanySearchResult.Items.Select(item => item.Title)
            .Should().Contain(["Senior AI Engineer", "Junior Frontend Developer"]);

        var caseInsensitiveSearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "SENIOR");

        caseInsensitiveSearchResult.TotalCount.Should().Be(2);
        caseInsensitiveSearchResult.Items.Should().HaveCount(2);

        var trimmedSearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "   senior   ");

        trimmedSearchResult.TotalCount.Should().Be(2);
        trimmedSearchResult.Items.Should().HaveCount(2);

        var companyOnlySearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "Cloud Infrastructure");

        companyOnlySearchResult.TotalCount.Should().Be(1);
        companyOnlySearchResult.Items[0].Title.Should().Be("DevOps Engineer");

        var pagedSearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 1,
            searchTerm: "Engineer");

        pagedSearchResult.TotalCount.Should().Be(2);
        pagedSearchResult.TotalPages.Should().Be(2);
        pagedSearchResult.Items.Should().HaveCount(1);

        var whitespaceSearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "   ");

        whitespaceSearchResult.TotalCount.Should().Be(4);
        whitespaceSearchResult.Items.Should().HaveCount(4);

        var nonMatchingSearchResult = await repository.GetEmployerJobsPaginatedAsync(
            "employer-1",
            status: null,
            isExpired: null,
            page: 1,
            pageSize: 10,
            searchTerm: "NonexistentQuery");

        nonMatchingSearchResult.TotalCount.Should().Be(0);
        nonMatchingSearchResult.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEmployerIdAsync_WhenEmployerHasSoftDeletedJob_ExcludesIt()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var activeJob = MakeEmployerJob("employer-1", "Active Role", "Acme Corp");
        var deletedJob = MakeEmployerJob("employer-1", "Deleted Role", "Acme Corp");
        deletedJob.SoftDelete();
        context.Jobs.AddRange(activeJob, deletedJob);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmployerIdAsync("employer-1");

        result.Should().ContainSingle().Which.Id.Should().Be(activeJob.Id);
    }

    [Fact]
    public async Task GetByEmployerIdIncludingDeletedAsync_WhenEmployerHasSoftDeletedJob_ReturnsIt()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var activeJob = MakeEmployerJob("employer-1", "Active Role", "Acme Corp");
        var deletedJob = MakeEmployerJob("employer-1", "Deleted Role", "Acme Corp");
        deletedJob.SoftDelete();
        context.Jobs.AddRange(activeJob, deletedJob);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmployerIdIncludingDeletedAsync("employer-1");

        result.Should().HaveCount(2);
        result.Select(j => j.Id).Should().Contain(deletedJob.Id);
    }

    private static Job MakeEmployerJob(
        string employerId,
        string title,
        string companyName,
        JobStatus status = JobStatus.Published,
        int daysUntilExpiry = 30)
    {
        var job = Job.Create(
            employerId,
            title,
            companyName,
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            1,
            DateTime.UtcNow.AddDays(daysUntilExpiry));

        if (status == JobStatus.Published)
        {
            job.MarkAsPublished();
        }
        else if (status == JobStatus.Closed)
        {
            job.MarkAsPublished();
            job.MarkAsClosed();
        }

        return job;
    }

    private static Job MakeCustomJob(
        string employerId = "employer-1",
        string title = "Backend Engineer",
        JobStatus status = JobStatus.Published,
        int daysUntilExpiry = 30)
    {
        var job = Job.Create(
            employerId,
            title,
            "Acme Corp",
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            1,
            DateTime.UtcNow.AddDays(daysUntilExpiry));

        if (status == JobStatus.Published)
        {
            job.MarkAsPublished();
        }
        else if (status == JobStatus.Closed)
        {
            job.MarkAsPublished();
            job.MarkAsClosed();
        }

        return job;
    }

    private static JobApplication MakeJobApplication(Guid jobId, string talentUserId = "talent-1")
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Applicant Name",
            "applicant@example.com",
            "resumes/resume.pdf");
    }
}


using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Models;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class JobRepositoryAdminJobsTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job MakeJob(
        JobStatus status,
        string title = "Backend Engineer",
        string company = "Acme Corp",
        string employerId = "employer-1",
        string location = "Dhaka",
        int daysUntilExpiry = 30)
    {
        var job = Job.Create(
            employerId,
            title,
            company,
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            location,
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            2,
            DateTime.UtcNow.AddDays(daysUntilExpiry));

        switch (status)
        {
            case JobStatus.Published:
                job.MarkAsPublished();
                break;
            case JobStatus.Closed:
                job.MarkAsPublished();
                job.MarkAsClosed();
                break;
            case JobStatus.Deactivated:
                job.MarkAsPublished();
                job.Deactivate(DeactivationReason.Spam, "Posting spam links to another site.");
                break;
            case JobStatus.PendingReview:
                job.MarkAsPublished();
                job.Deactivate(DeactivationReason.Spam, "Posting spam links to another site.");
                job.SubmitForReview();
                break;
        }

        return job;
    }

    private static User MakeEmployer(
        string userId = "employer-1",
        string name = "Ayesha Rahman",
        string email = "ayesha@acme.com")
    {
        return new User(userId, name, email, UserRole.Employer, isVerified: true);
    }

    private static AdminJobQueryParams Params(string? search = null, JobStatus? status = null, int page = 1, int pageSize = 20)
    {
        return new AdminJobQueryParams { SearchQuery = search, Status = status, Page = page, PageSize = pageSize };
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_ReturnsOnlyModeratedStatuses()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var softDeleted = MakeJob(JobStatus.Published, title: "Deleted Job");
        softDeleted.SoftDelete();

        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.AddRange(
            MakeJob(JobStatus.Published, title: "Published Job"),
            MakeJob(JobStatus.Deactivated, title: "Deactivated Job"),
            MakeJob(JobStatus.PendingReview, title: "Pending Job"),
            MakeJob(JobStatus.Draft, title: "Draft Job"),
            MakeJob(JobStatus.Closed, title: "Closed Job"),
            softDeleted);
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        result.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Title).Should().BeEquivalentTo(
            ["Published Job", "Deactivated Job", "Pending Job"]);
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_ReturnsBothTheFirstPostedAndTheLastPublishedDates()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakeJob(JobStatus.Published, title: "Reactivated Job");
        var firstPublishedAt = job.FirstPublishedAt;
        Thread.Sleep(5);
        job.Deactivate(DeactivationReason.Spam, "Posting spam links to another site.");
        job.Reactivate();
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        var item = result.Items.Single();
        item.FirstPublishedAt.Should().Be(firstPublishedAt);
        item.PublishedAt.Should().Be(job.PublishedAt);
        item.PublishedAt.Should().BeAfter(firstPublishedAt!.Value);
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_OrdersJobsAwaitingReviewFirst()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.AddRange(
            MakeJob(JobStatus.Published, title: "Published Job"),
            MakeJob(JobStatus.Deactivated, title: "Deactivated Job"),
            MakeJob(JobStatus.PendingReview, title: "Pending Job"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        result.Items[0].Title.Should().Be("Pending Job");
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_IncludesEmployerNameAndEmail()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.Add(MakeJob(JobStatus.Published));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        result.Items[0].EmployerName.Should().Be("Ayesha Rahman");
        result.Items[0].EmployerEmail.Should().Be("ayesha@acme.com");
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_WhenEmployerRecordIsMissing_StillReturnsJob()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.Jobs.Add(MakeJob(JobStatus.Published, employerId: "ghost-employer"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        result.TotalCount.Should().Be(1);
        result.Items[0].EmployerName.Should().BeEmpty();
        result.Items[0].EmployerEmail.Should().BeEmpty();
    }

    [Theory]
    [InlineData("backend")]
    [InlineData("acme")]
    [InlineData("dhaka")]
    [InlineData("ayesha")]
    [InlineData("ayesha@acme.com")]
    public async Task GetAdminJobsPagedAsync_SearchesTitleCompanyLocationAndEmployer(string searchQuery)
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.DomainUsers.AddRange(
            MakeEmployer(),
            MakeEmployer("employer-2", "Other Person", "other@example.com"));
        context.Jobs.AddRange(
            MakeJob(JobStatus.Published),
            MakeJob(JobStatus.Published, title: "Designer", company: "Globex", employerId: "employer-2", location: "Berlin"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params(search: searchQuery));

        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Backend Engineer");
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_FiltersByStatus()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.AddRange(
            MakeJob(JobStatus.Published, title: "Published Job"),
            MakeJob(JobStatus.Deactivated, title: "Deactivated Job"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params(status: JobStatus.Deactivated));

        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Deactivated Job");
        result.Items[0].DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_CountsApplicantsAndFlagsExpiredJobs()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var expiredJob = MakeJob(JobStatus.Published, daysUntilExpiry: -3);
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.Add(expiredJob);
        context.JobApplications.AddRange(
            new JobApplication(expiredJob.Id, "talent-1", "First Applicant", "first@example.com", "resumes/a.pdf"),
            new JobApplication(expiredJob.Id, "talent-2", "Second Applicant", "second@example.com", "resumes/b.pdf"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params());

        result.Items[0].IsExpired.Should().BeTrue();
        result.Items[0].ApplicantCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAdminJobsPagedAsync_AppliesPaging()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.DomainUsers.Add(MakeEmployer());
        context.Jobs.AddRange(
            MakeJob(JobStatus.Published, title: "Job One"),
            MakeJob(JobStatus.Published, title: "Job Two"),
            MakeJob(JobStatus.Published, title: "Job Three"));
        await context.SaveChangesAsync();

        var result = await repository.GetAdminJobsPagedAsync(Params(page: 2, pageSize: 2));

        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(1);
    }
}

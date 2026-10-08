using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetJobApplicants;

public class GetJobApplicantsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly GetJobApplicantsQueryHandler _handler;

    public GetJobApplicantsQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.Setup(u => u.Talents).Returns(_talentRepository.Object);
        _talentRepository
            .Setup(r => r.GetByUserIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _presignedUrlService
            .Setup(p => p.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, TimeSpan expiry, CancellationToken _) => $"https://storage.test/{key}?expiry={expiry.TotalMinutes}");

        _handler = new GetJobApplicantsQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _presignedUrlService.Object);
    }

    private static Job CreateJob(string employerId)
    {
        return Job.Create(
            employerId,
            "Senior Software Engineer",
            "Tech Corp",
            "Building scalable distributed systems.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka, Bangladesh",
            WorkArrangement.Hybrid,
            new List<string> { "C#", ".NET" },
            2,
            DateTime.UtcNow.AddDays(30));
    }

    private static JobApplication CreateApplication(Guid jobId, string talentUserId, string name, string email, string? storageKey = null)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            name,
            email,
            storageKey ?? "resumes/test_cv.pdf",
            "Cover letter content",
            120000m,
            6m,
            "Dhaka");
    }

    [Fact]
    public async Task Handle_WhenCallerIsEmployerWhoOwnsJob_ReturnsPaginatedApplicantList()
    {
        var job = CreateJob("employer-1");
        var app1 = CreateApplication(job.Id, "user-1", "Alice Smith", "alice@example.com", "resumes/alice.pdf");
        var app2 = CreateApplication(job.Id, "user-2", "Bob Jones", "bob@example.com", "resumes/bob.pdf");
        var items = new List<JobApplication> { app1, app2 };
        var paginated = new PaginatedList<JobApplication>(items, 1, 10, 2);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var query = new GetJobApplicantsQuery(job.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);

        var item1 = result.Items.First(i => i.Id == app1.Id);
        item1.ApplicantName.Should().Be("Alice Smith");
        item1.ApplicantEmail.Should().Be("alice@example.com");
        item1.ResumeDownloadUrl.Should().Be("https://storage.test/resumes/alice.pdf?expiry=60");
        item1.Status.Should().Be("Applied");
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthorized_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var query = new GetJobApplicantsQuery(Guid.NewGuid());

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be authenticated to view job applicants.");
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _jobRepository
            .Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var query = new GetJobApplicantsQuery(jobId);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job '{jobId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnJob_ThrowsForbiddenException()
    {
        var job = CreateJob("other-employer");
        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var query = new GetJobApplicantsQuery(job.Id);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view applicants for this job.");
    }

    [Fact]
    public async Task Handle_AppliesSearchStatusFilterAndClampsPaginationParameters()
    {
        var job = CreateJob("employer-1");
        var app = CreateApplication(job.Id, "user-1", "Alice Smith", "alice@example.com");
        var paginated = new PaginatedList<JobApplication>(new List<JobApplication> { app }, 1, 10, 1);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, "Alice", ApplicationStatus.Interview, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var query = new GetJobApplicantsQuery(
            JobId: job.Id,
            Search: "Alice",
            Status: ApplicationStatus.Interview,
            Page: 0, // Should clamp to 1
            PageSize: 150 // Should clamp to 100
        );

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, "Alice", ApplicationStatus.Interview, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _jobApplicationRepository.Verify(
            r => r.GetByJobIdPaginatedAsync(job.Id, "Alice", ApplicationStatus.Interview, null, null, 1, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResumeStorageKeyIsEmpty_ReturnsNullResumeDownloadUrl()
    {
        var job = CreateJob("employer-1");
        var app = CreateApplication(job.Id, "user-1", "Alice Smith", "alice@example.com", storageKey: "");
        var paginated = new PaginatedList<JobApplication>(new List<JobApplication> { app }, 1, 10, 1);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var query = new GetJobApplicantsQuery(job.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.First().ResumeDownloadUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTalentRenamedAfterApply_ShowsCurrentTalentName()
    {
        var job = CreateJob("employer-1");
        var app = CreateApplication(job.Id, "user-1", "Old Name", "alice@example.com");
        var paginated = new PaginatedList<JobApplication>(new List<JobApplication> { app }, 1, 10, 1);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var renamedTalent = new Talent("user-1", "Alice Renamed", "alice@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdsAsync(new List<string> { "user-1" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync([renamedTalent]);

        var result = await _handler.Handle(new GetJobApplicantsQuery(job.Id), CancellationToken.None);

        var item = result.Items.First();
        item.ApplicantName.Should().Be("Alice Renamed");
        item.ApplicantEmail.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_WhenTalentProfileIsMissing_FallsBackToDenormalizedApplicationName()
    {
        var job = CreateJob("employer-1");
        var app1 = CreateApplication(job.Id, "user-1", "Alice Smith", "alice@example.com");
        var app2 = CreateApplication(job.Id, "user-2", "Bob Jones", "bob@example.com");
        var paginated = new PaginatedList<JobApplication>(new List<JobApplication> { app1, app2 }, 1, 10, 2);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(job.Id, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var onlyFirstTalent = new Talent("user-1", "Alice Smith", "alice@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([onlyFirstTalent]);

        var result = await _handler.Handle(new GetJobApplicantsQuery(job.Id), CancellationToken.None);

        var item1 = result.Items.First(i => i.Id == app1.Id);
        var item2 = result.Items.First(i => i.Id == app2.Id);
        item1.ApplicantName.Should().Be("Alice Smith");
        item2.ApplicantName.Should().Be("Bob Jones");
        item2.ApplicantEmail.Should().Be("bob@example.com");
    }
}

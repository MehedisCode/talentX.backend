using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class GetPreferredJobsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPreferredJobsQueryHandler _handler;

    public GetPreferredJobsQueryHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.SetupGet(unit => unit.Jobs).Returns(_jobRepository.Object);

        _mapper.Setup(mapper => mapper.Map<List<JobListItemDto>>(It.IsAny<List<Job>>()))
            .Returns((List<Job> source) => source.Select(job => new JobListItemDto
            {
                Id = job.Id,
                Title = job.Title,
                CompanyName = job.CompanyName,
                Location = job.Location,
                EmploymentType = job.EmploymentType.ToString(),
                ExperienceLevel = job.ExperienceLevel.ToString(),
                WorkArrangement = job.WorkArrangement.ToString(),
                CreatedAt = job.PublishedAt ?? job.CreatedAt,
                ExpireAt = job.ExpireAt
            }).ToList());

        _handler = new GetPreferredJobsQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _mapper.Object);
    }

    private static Job CreatePublishedJob(
        string title = "Senior Backend Engineer",
        string location = "Dhaka",
        EmploymentType employmentType = EmploymentType.FullTime,
        ExperienceLevel experienceLevel = ExperienceLevel.SeniorLevel,
        WorkArrangement workArrangement = WorkArrangement.FullRemote,
        DateTime? expireAt = null)
    {
        var job = Job.Create(
            "employer-1",
            title,
            "Acme Corp",
            "Description of the job role",
            employmentType,
            experienceLevel,
            location,
            workArrangement,
            new List<string> { ".NET", "C#" },
            2,
            expireAt ?? DateTime.UtcNow.AddDays(30));

        job.MarkAsPublished();
        return job;
    }

    [Fact]
    public async Task Handle_WithSpecificAlertId_MatchesJobsAgainstSpecificAlert()
    {
        var alert = JobAlert.Create(
            "talent-user-1",
            "C# Alert",
            "Backend",
            "Dhaka",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            WorkArrangement.FullRemote);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var matchingJob = CreatePublishedJob(title: "Senior Backend Developer", location: "Dhaka");
        var nonMatchingJob = CreatePublishedJob(title: "Frontend Developer", location: "Dhaka");

        _jobRepository
            .Setup(repository => repository.GetActivePublishedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { matchingJob, nonMatchingJob });

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams(AlertId: alert.Id, Page: 1, PageSize: 10));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(matchingJob.Id);
        result.Items[0].Title.Should().Be(matchingJob.Title);
    }

    [Fact]
    public async Task Handle_WithoutAlertId_MatchesJobsAcrossAllActiveAlertsAndDeduplicates()
    {
        var alert1 = JobAlert.Create(
            "talent-user-1",
            "Alert 1",
            "Engineer",
            null,
            null,
            null,
            null);

        var alert2 = JobAlert.Create(
            "talent-user-1",
            "Alert 2",
            "Backend",
            null,
            null,
            null,
            null);

        _jobAlertRepository
            .Setup(repository => repository.GetActiveAlertsByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobAlert> { alert1, alert2 });

        // This job matches both alert1 (title contains "Engineer") and alert2 (title contains "Backend")
        var dualMatchingJob = CreatePublishedJob(title: "Backend Engineer");
        var singleMatchingJob = CreatePublishedJob(title: "DevOps Engineer");
        var nonMatchingJob = CreatePublishedJob(title: "UI Designer");

        _jobRepository
            .Setup(repository => repository.GetActivePublishedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { dualMatchingJob, singleMatchingJob, nonMatchingJob });

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams(AlertId: null, Page: 1, PageSize: 10));

        var result = await _handler.Handle(query, CancellationToken.None);

        // dualMatchingJob should appear only once (deduplicated)
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(item => item.Id).Should().Contain([dualMatchingJob.Id, singleMatchingJob.Id]);
    }

    [Fact]
    public async Task Handle_WhenNoActiveAlerts_ReturnsEmptyPaginatedList()
    {
        _jobAlertRepository
            .Setup(repository => repository.GetActiveAlertsByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobAlert>());

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        _jobRepository.Verify(repository => repository.GetActivePublishedJobsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSpecificAlertNotFound_ThrowsNotFoundException()
    {
        var missingAlertId = Guid.NewGuid();

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(missingAlertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobAlert?)null);

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams(AlertId: missingAlertId));

        var action = () => _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSpecificAlertNotOwnedByCaller_ThrowsForbiddenException()
    {
        var otherAlert = JobAlert.Create(
            "different-talent-user",
            "Alert",
            "Engineer",
            null,
            null,
            null,
            null);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(otherAlert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherAlert);

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams(AlertId: otherAlert.Id));

        var action = () => _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams());

        var action = () => _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPageSlice()
    {
        var alert = JobAlert.Create(
            "talent-user-1",
            "All Engineers",
            "Engineer",
            null,
            null,
            null,
            null);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var job1 = CreatePublishedJob(title: "Engineer 1");
        var job2 = CreatePublishedJob(title: "Engineer 2");
        var job3 = CreatePublishedJob(title: "Engineer 3");

        _jobRepository
            .Setup(repository => repository.GetActivePublishedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job1, job2, job3 });

        var query = new GetPreferredJobsQuery(new PreferredJobsQueryParams(AlertId: alert.Id, Page: 2, PageSize: 2));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(1);
    }
}

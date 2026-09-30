using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetJobPipelineSummary;

public sealed class GetJobPipelineSummaryQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetJobPipelineSummaryQueryHandler _handler;

    public GetJobPipelineSummaryQueryHandlerTests()
    {
        _unitOfWork.Setup(work => work.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(work => work.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-1");

        _handler = new GetJobPipelineSummaryQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
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
            ["C#", ".NET"],
            2,
            DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthorized_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var query = new GetJobPipelineSummaryQuery(Guid.NewGuid());

        var handleAction = () => _handler.Handle(query, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be authenticated to view the job pipeline summary.");
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var query = new GetJobPipelineSummaryQuery(jobId);

        var handleAction = () => _handler.Handle(query, CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job '{jobId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnJob_ThrowsForbiddenException()
    {
        var job = CreateJob("other-employer");
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var query = new GetJobPipelineSummaryQuery(job.Id);

        var handleAction = () => _handler.Handle(query, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view the pipeline summary for this job.");
    }

    [Fact]
    public async Task Handle_WhenJobExistsAndOwned_ReturnsJobPipelineSummaryDto()
    {
        var job = CreateJob("employer-1");
        job.MarkAsPublished();

        var expectedPipelineStats = new CandidatePipelineStatsDto(
            AppliedCount: 5,
            ShortlistedCount: 3,
            InvitedCount: 2,
            RejectedCount: 1,
            HiredCount: 1);

        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(repository => repository.GetCandidatePipelineStatsByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPipelineStats);

        var query = new GetJobPipelineSummaryQuery(job.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.JobId.Should().Be(job.Id);
        result.JobTitle.Should().Be("Senior Software Engineer");
        result.CompanyName.Should().Be("Tech Corp");
        result.JobStatus.Should().Be("Published");
        result.Vacancies.Should().Be(2);
        result.PostedDate.Should().Be(job.PublishedAt!.Value);
        result.TotalApplicants.Should().Be(12);
        result.PipelineStats.Should().Be(expectedPipelineStats);
    }

    [Fact]
    public async Task Handle_WhenJobHasZeroApplicants_ReturnsZeroCountsAndTotal()
    {
        var job = CreateJob("employer-1");

        var zeroPipelineStats = new CandidatePipelineStatsDto(
            AppliedCount: 0,
            ShortlistedCount: 0,
            InvitedCount: 0,
            RejectedCount: 0,
            HiredCount: 0);

        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _jobApplicationRepository
            .Setup(repository => repository.GetCandidatePipelineStatsByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zeroPipelineStats);

        var query = new GetJobPipelineSummaryQuery(job.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalApplicants.Should().Be(0);
        result.PostedDate.Should().Be(job.CreatedAt);
        result.PipelineStats.AppliedCount.Should().Be(0);
        result.PipelineStats.ShortlistedCount.Should().Be(0);
        result.PipelineStats.InvitedCount.Should().Be(0);
        result.PipelineStats.RejectedCount.Should().Be(0);
        result.PipelineStats.HiredCount.Should().Be(0);
    }
}

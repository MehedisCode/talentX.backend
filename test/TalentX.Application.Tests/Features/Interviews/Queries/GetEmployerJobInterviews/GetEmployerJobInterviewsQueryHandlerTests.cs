using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews.Queries.GetEmployerJobInterviews;

public class GetEmployerJobInterviewsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetEmployerJobInterviewsQueryHandler _handler;

    public GetEmployerJobInterviewsQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(u => u.InterviewInvitations).Returns(_interviewInvitationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");

        _handler = new GetEmployerJobInterviewsQueryHandler(
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
            new List<string> { "C#", ".NET" },
            2,
            DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_BuildsFilterFromStatusAndTypeAndPassesItToRepository()
    {
        var job = CreateJob("employer-1");
        var paginated = new PaginatedList<InterviewInvitation>(new List<InterviewInvitation>(), 1, 20, 0);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _interviewInvitationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(
                job.Id,
                It.Is<InterviewInvitationListFilter>(f =>
                    f.Status == InterviewInvitationStatus.Accepted && f.Type == InterviewType.OnSite),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var query = new GetEmployerJobInterviewsQuery(
            JobId: job.Id,
            Status: InterviewInvitationStatus.Accepted,
            Type: InterviewType.OnSite);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _interviewInvitationRepository.Verify(
            r => r.GetByJobIdPaginatedAsync(
                job.Id,
                It.Is<InterviewInvitationListFilter>(f =>
                    f.Status == InterviewInvitationStatus.Accepted && f.Type == InterviewType.OnSite),
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoStatusOrTypeGiven_PassesEmptyFilter()
    {
        var job = CreateJob("employer-1");
        var paginated = new PaginatedList<InterviewInvitation>(new List<InterviewInvitation>(), 1, 20, 0);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _interviewInvitationRepository
            .Setup(r => r.GetByJobIdPaginatedAsync(
                job.Id,
                It.Is<InterviewInvitationListFilter>(f => f.Status == null && f.Type == null),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var query = new GetEmployerJobInterviewsQuery(job.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthorized_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var query = new GetEmployerJobInterviewsQuery(Guid.NewGuid());

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be logged in to view job interview invitations.");
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _jobRepository
            .Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var query = new GetEmployerJobInterviewsQuery(jobId);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job with ID '{jobId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnJob_ThrowsForbiddenException()
    {
        var job = CreateJob("other-employer");
        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var query = new GetEmployerJobInterviewsQuery(job.Id);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You cannot view interview invitations for jobs you do not own.");
    }
}

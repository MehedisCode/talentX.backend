using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Queries.GetApplicationStatusHistory;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetApplicationStatusHistory;

public class GetApplicationStatusHistoryQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IJobApplicationStatusHistoryRepository> _historyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetApplicationStatusHistoryQueryHandler _handler;

    public GetApplicationStatusHistoryQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplicationStatusHistories).Returns(_historyRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");

        _handler = new GetApplicationStatusHistoryQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    private static JobApplication CreateApplication(Guid jobId, string talentUserId)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Alex Smith",
            "alex@example.com",
            "resumes/alex.pdf");
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(
            new GetApplicationStatusHistoryQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be authenticated to view application status history.");
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundException()
    {
        var applicationId = Guid.NewGuid();
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        var act = () => _handler.Handle(
            new GetApplicationStatusHistoryQuery(applicationId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job application '{applicationId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenApplicationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var application = CreateApplication(Guid.NewGuid(), "talent-2");
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var act = () => _handler.Handle(
            new GetApplicationStatusHistoryQuery(application.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view status history for this job application.");
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsApplication_ReturnsHistoryOldestFirst()
    {
        var application = CreateApplication(Guid.NewGuid(), "talent-1");
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var histories = new List<JobApplicationStatusHistory>
        {
            JobApplicationStatusHistory.Create(application.Id, null, ApplicationStatus.Applied, "alex@example.com"),
            JobApplicationStatusHistory.Create(application.Id, ApplicationStatus.Applied, ApplicationStatus.Shortlisted, "employer-1"),
        };
        _historyRepository
            .Setup(r => r.GetByApplicationIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(histories);

        var result = await _handler.Handle(
            new GetApplicationStatusHistoryQuery(application.Id),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].FromStatus.Should().BeNull();
        result[0].ToStatus.Should().Be("Applied");
        result[1].FromStatus.Should().Be("Applied");
        result[1].ToStatus.Should().Be("Shortlisted");
        result[0].ChangedAt.Should().BeOnOrBefore(result[1].ChangedAt);
    }

    [Fact]
    public async Task Handle_WhenNoHistoryExists_ReturnsEmptyList()
    {
        var application = CreateApplication(Guid.NewGuid(), "talent-1");
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _historyRepository
            .Setup(r => r.GetByApplicationIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplicationStatusHistory>());

        var result = await _handler.Handle(
            new GetApplicationStatusHistoryQuery(application.Id),
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}

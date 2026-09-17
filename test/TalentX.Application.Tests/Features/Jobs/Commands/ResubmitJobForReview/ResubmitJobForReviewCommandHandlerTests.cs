using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ResubmitJobForReview;

public sealed class ResubmitJobForReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobModerationHistoryRepository> _historyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ResubmitJobForReviewCommandHandler _handler;

    public ResubmitJobForReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.JobModerationHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-1");
        _currentUserService.SetupGet(service => service.Email).Returns("ayesha@acme.com");
        _handler = new ResubmitJobForReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    // A job that satisfies every publish rule, then taken down by an admin.
    private static Job CreateCompleteDeactivatedJob(string employerId = "employer-1", bool withRequirements = true)
    {
        var job = Job.Create(
            employerId,
            "Senior Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#"],
            2,
            DateTime.UtcNow.AddDays(30));
        job.UpdateWizardDetails(
            null,
            null,
            4,
            null,
            SalaryType.Negotiable,
            requirements: withRequirements ? "- 3-5 years of relevant experience" : null);
        job.MarkAsPublished();
        job.Deactivate(DeactivationReason.MisleadingInformation, "Salary does not match the description.");
        return job;
    }

    private void SetupJob(Job job)
    {
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    private void VerifyNothingWritten()
    {
        _historyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<JobModerationHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _auditLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOwnerResubmitsACompleteDeactivatedJob_MovesItToPendingReviewInOneSave()
    {
        var job = CreateCompleteDeactivatedJob();
        SetupJob(job);

        await _handler.Handle(new ResubmitJobForReviewCommand(job.Id), CancellationToken.None);

        job.Status.Should().Be(JobStatus.PendingReview);
        // The reason stays visible while the admin reviews the fix.
        job.DeactivationReason.Should().Be(DeactivationReason.MisleadingInformation);

        _historyRepository.Verify(
            repository => repository.AddAsync(
                It.Is<JobModerationHistory>(history =>
                    history.JobId == job.Id &&
                    history.Action == JobModerationAction.ResubmittedForReview &&
                    history.FromStatus == JobStatus.Deactivated &&
                    history.ToStatus == JobStatus.PendingReview &&
                    history.ActorUserId == "employer-1" &&
                    history.ActorEmail == "ayesha@acme.com" &&
                    history.Reason == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.Actor == "ayesha@acme.com" &&
                    log.Action == "Resubmitted job 'Senior Backend Engineer' for review"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnTheJob_ThrowsForbiddenAndWritesNothing()
    {
        var job = CreateCompleteDeactivatedJob(employerId: "someone-else");
        SetupJob(job);

        var handleAction = () => _handler.Handle(new ResubmitJobForReviewCommand(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to resubmit this job.");
        job.Status.Should().Be(JobStatus.Deactivated);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Handle_WhenJobIsNotDeactivated_ThrowsBadRequestAndWritesNothing()
    {
        var job = CreateCompleteDeactivatedJob();
        job.Reactivate();
        SetupJob(job);

        var handleAction = () => _handler.Handle(new ResubmitJobForReviewCommand(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only deactivated jobs can be resubmitted for review.");
        job.Status.Should().Be(JobStatus.Published);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Handle_WhenJobIsIncomplete_ThrowsValidationErrorAndStaysDeactivated()
    {
        var job = CreateCompleteDeactivatedJob(withRequirements: false);
        SetupJob(job);

        var handleAction = () => _handler.Handle(new ResubmitJobForReviewCommand(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*Requirements*");
        job.Status.Should().Be(JobStatus.Deactivated);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFound()
    {
        var missingJobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(missingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var handleAction = () => _handler.Handle(new ResubmitJobForReviewCommand(missingJobId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
        VerifyNothingWritten();
    }

    [Theory]
    [InlineData(null, "ayesha@acme.com")]
    [InlineData("employer-1", null)]
    public async Task Handle_WhenCallerIdentityIsMissing_ThrowsUnauthorizedAndTouchesNothing(string? userId, string? email)
    {
        _currentUserService.SetupGet(service => service.UserId).Returns(userId);
        _currentUserService.SetupGet(service => service.Email).Returns(email);

        var handleAction = () => _handler.Handle(new ResubmitJobForReviewCommand(Guid.NewGuid()), CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to resubmit a job.");
        _jobRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNothingWritten();
    }
}

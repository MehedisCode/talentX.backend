using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.Commands.ReactivateJob;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ReactivateJob;

public sealed class ReactivateJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJobModerationHistoryRepository> _historyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IJobAlertQueue> _jobAlertQueue = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly ReactivateJobCommandHandler _handler;

    public ReactivateJobCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.JobModerationHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.com");
        _emailService
            .Setup(service => service.RenderJobReactivatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>job reactivated</html>");

        _handler = new ReactivateJobCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object,
            _jobAlertQueue.Object,
            _notificationService.Object,
            Mock.Of<ILogger<ReactivateJobCommandHandler>>());
    }

    private static Job CreateDeactivatedJob(DateTime? expireAt = null, bool pendingReview = false)
    {
        var job = Job.Create(
            "employer-1",
            "Senior Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#"],
            2,
            expireAt ?? DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        job.Deactivate(DeactivationReason.Spam, "Posting links to an unrelated external site.");
        if (pendingReview)
        {
            job.SubmitForReview();
        }

        return job;
    }

    private void SetupJob(Job job)
    {
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    private void SetupEmployer()
    {
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("employer-1"))
            .ReturnsAsync(new User("employer-1", "Ayesha Rahman", "ayesha@acme.com", UserRole.Employer, isVerified: true));
    }

    [Fact]
    public async Task Handle_WhenJobIsDeactivated_PublishesAndWritesHistoryAuditLogAndEmailInOneSave()
    {
        var job = CreateDeactivatedJob();
        SetupJob(job);
        SetupEmployer();

        var result = await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        result.Message.Should().Be("Job reactivated.");
        job.Status.Should().Be(JobStatus.Published);
        job.DeactivationReason.Should().BeNull();
        job.DeactivationDetails.Should().BeNull();

        _historyRepository.Verify(
            repository => repository.AddAsync(
                It.Is<JobModerationHistory>(history =>
                    history.Action == JobModerationAction.Reactivated &&
                    history.FromStatus == JobStatus.Deactivated &&
                    history.ToStatus == JobStatus.Published &&
                    history.Reason == null &&
                    history.Details == null &&
                    history.ActorEmail == "admin@talentx.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log => log.Action == "Reactivated job 'Senior Backend Engineer'"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailService.Verify(
            service => service.RenderJobReactivatedEmailAsync(
                "Ayesha Rahman",
                "Senior Backend Engineer",
                It.Is<string>(url => url.EndsWith($"/jobs/{job.Id}")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailDispatchLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<EmailDispatchLog>(email =>
                    email.RecipientEmail == "ayesha@acme.com" &&
                    email.EmailType == "JobReactivated" &&
                    email.Subject.Contains("Senior Backend Engineer")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJobIsPendingReview_Publishes()
    {
        var job = CreateDeactivatedJob(pendingReview: true);
        SetupJob(job);
        SetupEmployer();

        await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        job.Status.Should().Be(JobStatus.Published);
        _historyRepository.Verify(
            repository => repository.AddAsync(
                It.Is<JobModerationHistory>(history => history.FromStatus == JobStatus.PendingReview),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PublishesAJobReactivatedNotificationToTheEmployerAfterTheSave()
    {
        var job = CreateDeactivatedJob();
        SetupJob(job);
        SetupEmployer();
        var callOrder = new List<string>();
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);
        _notificationService
            .Setup(service => service.PublishAsync(It.IsAny<PublishNotificationParameters>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("notify"))
            .ReturnsAsync(default(NotificationDto)!);

        await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        _notificationService.Verify(
            service => service.PublishAsync(
                It.Is<PublishNotificationParameters>(parameters =>
                    parameters.RecipientUserId == "employer-1" &&
                    parameters.Type == NotificationType.JobReactivated &&
                    parameters.Title == "Job Reactivated" &&
                    parameters.Body == "Your job 'Senior Backend Engineer' is live again." &&
                    parameters.Metadata!.Contains(job.Id.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
        // PublishAsync commits on its own, so it must never run inside the moderation save.
        callOrder.Should().Equal("save", "notify");
    }

    [Fact]
    public async Task Handle_WhenPublishingTheNotificationFails_StillReportsSuccessAndEnqueuesAlerts()
    {
        var job = CreateDeactivatedJob();
        SetupJob(job);
        SetupEmployer();
        _notificationService
            .Setup(service => service.PublishAsync(It.IsAny<PublishNotificationParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var result = await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        result.Message.Should().Be("Job reactivated.");
        job.Status.Should().Be(JobStatus.Published);
        _jobAlertQueue.Verify(
            queue => queue.EnqueuePublishedJobAsync(job.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EnqueuesJobAlertsOnlyAfterTheSaveSucceeds()
    {
        var job = CreateDeactivatedJob();
        SetupJob(job);
        SetupEmployer();
        var callOrder = new List<string>();
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);
        _jobAlertQueue
            .Setup(queue => queue.EnqueuePublishedJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("enqueue"))
            .Returns(ValueTask.CompletedTask);

        await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        callOrder.Should().Equal("save", "enqueue");
    }

    [Fact]
    public async Task Handle_WhenAlertQueueFails_StillReportsSuccess()
    {
        var job = CreateDeactivatedJob();
        SetupJob(job);
        SetupEmployer();
        _jobAlertQueue
            .Setup(queue => queue.EnqueuePublishedJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Queue is unavailable."));

        var result = await _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        result.Message.Should().Be("Job reactivated.");
        job.Status.Should().Be(JobStatus.Published);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeadlineHasExpired_ThrowsBadRequestAndWritesNothing()
    {
        var job = CreateDeactivatedJob(expireAt: DateTime.UtcNow.AddDays(-2));
        SetupJob(job);

        var handleAction = () => _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cannot reactivate a job with an expired deadline. The employer must update the application deadline first.");
        job.Status.Should().Be(JobStatus.Deactivated);
        _historyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<JobModerationHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationService.Verify(
            service => service.PublishAsync(It.IsAny<PublishNotificationParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(
            queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobIsAlreadyPublished_ThrowsBadRequest()
    {
        var job = CreateDeactivatedJob();
        job.Reactivate();
        SetupJob(job);

        var handleAction = () => _handler.Handle(new ReactivateJobCommand(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only deactivated or under-review jobs can be reactivated.");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFound()
    {
        var missingJobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(missingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var handleAction = () => _handler.Handle(new ReactivateJobCommand(missingJobId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null, "admin@talentx.com")]
    [InlineData("admin-1", null)]
    public async Task Handle_WhenCallerIdentityIsMissing_ThrowsUnauthorizedAndTouchesNothing(string? userId, string? email)
    {
        _currentUserService.SetupGet(service => service.UserId).Returns(userId);
        _currentUserService.SetupGet(service => service.Email).Returns(email);

        var handleAction = () => _handler.Handle(new ReactivateJobCommand(Guid.NewGuid()), CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to reactivate a job.");
        _jobRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(
            queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

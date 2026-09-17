using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.Commands.DeactivateJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.DeactivateJob;

public sealed class DeactivateJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJobModerationHistoryRepository> _historyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly DeactivateJobCommandHandler _handler;

    public DeactivateJobCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.JobModerationHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.com");
        _emailService
            .Setup(service => service.RenderJobDeactivatedEmailAsync(
                It.IsAny<RenderJobDeactivatedEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>job deactivated</html>");

        _handler = new DeactivateJobCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object,
            Mock.Of<ILogger<DeactivateJobCommandHandler>>());
    }

    private static Job CreateJob(string employerId = "employer-1")
    {
        return Job.Create(
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
    }

    private void SetupJob(Job job)
    {
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    private void SetupEmployer(string userId = "employer-1")
    {
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new User(userId, "Ayesha Rahman", "ayesha@acme.com", UserRole.Employer, isVerified: true));
    }

    private static DeactivateJobCommand Command(Guid jobId, string details = "Posting links to an unrelated external site.")
    {
        return new DeactivateJobCommand(jobId, DeactivationReason.MisleadingInformation, details);
    }

    [Fact]
    public async Task Handle_WhenJobIsPublished_DeactivatesAndWritesHistoryAuditLogAndEmailInOneSave()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        SetupJob(job);
        SetupEmployer();

        var result = await _handler.Handle(Command(job.Id), CancellationToken.None);

        result.Message.Should().Be("Job deactivated.");
        job.Status.Should().Be(JobStatus.Deactivated);
        job.DeactivationReason.Should().Be(DeactivationReason.MisleadingInformation);
        job.DeactivationDetails.Should().Be("Posting links to an unrelated external site.");

        _historyRepository.Verify(
            repository => repository.AddAsync(
                It.Is<JobModerationHistory>(history =>
                    history.JobId == job.Id &&
                    history.Action == JobModerationAction.Deactivated &&
                    history.FromStatus == JobStatus.Published &&
                    history.ToStatus == JobStatus.Deactivated &&
                    history.Reason == DeactivationReason.MisleadingInformation &&
                    history.Details == "Posting links to an unrelated external site." &&
                    history.ActorUserId == "admin-1" &&
                    history.ActorEmail == "admin@talentx.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.Actor == "admin@talentx.com" &&
                    log.Action == "Deactivated job 'Senior Backend Engineer' (Misleading or invalid information)"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailService.Verify(
            service => service.RenderJobDeactivatedEmailAsync(
                It.Is<RenderJobDeactivatedEmailParameters>(parameters =>
                    parameters.RecipientName == "Ayesha Rahman" &&
                    parameters.JobTitle == "Senior Backend Engineer" &&
                    parameters.ReasonLabel == "Misleading or invalid information" &&
                    parameters.Details == "Posting links to an unrelated external site." &&
                    parameters.EditJobUrl.EndsWith($"/job/{job.Id}/edit")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailDispatchLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<EmailDispatchLog>(email =>
                    email.RecipientEmail == "ayesha@acme.com" &&
                    email.RecipientName == "Ayesha Rahman" &&
                    email.EmailType == "JobDeactivated" &&
                    email.Subject.Contains("Senior Backend Engineer") &&
                    email.HtmlBody == "<html>job deactivated</html>"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJobIsPendingReview_DeactivatesAgainWithTheNewReason()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        job.Deactivate(DeactivationReason.Spam, "Original reason for deactivation.");
        job.SubmitForReview();
        SetupJob(job);
        SetupEmployer();

        await _handler.Handle(Command(job.Id, "Salary still does not match the description."), CancellationToken.None);

        job.Status.Should().Be(JobStatus.Deactivated);
        job.DeactivationReason.Should().Be(DeactivationReason.MisleadingInformation);
        job.DeactivationDetails.Should().Be("Salary still does not match the description.");
        _historyRepository.Verify(
            repository => repository.AddAsync(
                It.Is<JobModerationHistory>(history => history.FromStatus == JobStatus.PendingReview),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TrimsDetailsBeforeStoringThem()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        SetupJob(job);
        SetupEmployer();

        await _handler.Handle(Command(job.Id, "   Posting spam links everywhere.   "), CancellationToken.None);

        job.DeactivationDetails.Should().Be("Posting spam links everywhere.");
    }

    [Fact]
    public async Task Handle_WhenJobIsDraft_ThrowsBadRequestAndWritesNothing()
    {
        var job = CreateJob();
        SetupJob(job);

        var handleAction = () => _handler.Handle(Command(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only published or under-review jobs can be deactivated.");
        job.Status.Should().Be(JobStatus.Draft);
        _historyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<JobModerationHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _auditLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailDispatchLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundAndDoesNotSave()
    {
        var missingJobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(missingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var handleAction = () => _handler.Handle(Command(missingJobId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmployerRecordIsMissing_StillDeactivatesWithoutQueuingEmail()
    {
        var job = CreateJob("ghost-employer");
        job.MarkAsPublished();
        SetupJob(job);
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("ghost-employer"))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(Command(job.Id), CancellationToken.None);

        result.Message.Should().Be("Job deactivated.");
        job.Status.Should().Be(JobStatus.Deactivated);
        _emailService.Verify(
            service => service.RenderJobDeactivatedEmailAsync(
                It.IsAny<RenderJobDeactivatedEmailParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailDispatchLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

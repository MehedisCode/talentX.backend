using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobReports.Commands.ResolveJobReport;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Application.Features.Jobs.Moderation;
using Xunit;

namespace TalentX.Application.Tests.Features.JobReports.Commands.ResolveJobReport;

public class ResolveJobReportCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ICurrentUserService> _currentUserService;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<ILogger<ResolveJobReportCommandHandler>> _logger;
    private readonly ResolveJobReportCommandHandler _handler;

    private readonly Mock<IJobReportRepository> _jobReportRepository;
    private readonly Mock<IJobRepository> _jobRepository;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IJobModerationHistoryRepository> _historyRepository;
    private readonly Mock<IAuditLogRepository> _auditLogRepository;
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository;

    public ResolveJobReportCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUserService = new Mock<ICurrentUserService>();
        _emailService = new Mock<IEmailService>();
        _notificationService = new Mock<INotificationService>();
        _logger = new Mock<ILogger<ResolveJobReportCommandHandler>>();

        _jobReportRepository = new Mock<IJobReportRepository>();
        _jobRepository = new Mock<IJobRepository>();
        _userRepository = new Mock<IUserRepository>();
        _historyRepository = new Mock<IJobModerationHistoryRepository>();
        _auditLogRepository = new Mock<IAuditLogRepository>();
        _emailDispatchLogRepository = new Mock<IEmailDispatchLogRepository>();

        _unitOfWork.SetupGet(u => u.JobReports).Returns(_jobReportRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.JobModerationHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(u => u.AuditLogs).Returns(_auditLogRepository.Object);
        _unitOfWork.SetupGet(u => u.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);

        _handler = new ResolveJobReportCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object,
            _notificationService.Object,
            _logger.Object
        );
    }

    [Theory]
    [InlineData(null, "admin@test.com")]
    [InlineData("admin-1", null)]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException(string? userId, string? email)
    {
        _currentUserService.SetupGet(s => s.UserId).Returns(userId);
        _currentUserService.SetupGet(s => s.Email).Returns(email);

        var command = new ResolveJobReportCommand(Guid.NewGuid(), DeactivationReason.Spam, "Details", null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in as admin.");
    }

    [Fact]
    public async Task Handle_ReportNotFound_ThrowsNotFoundException()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var reportId = Guid.NewGuid();
        _jobReportRepository.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobReport?)null);

        var command = new ResolveJobReportCommand(reportId, DeactivationReason.Spam, "Details", null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReportNotPending_ThrowsBadRequestException()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var report = new JobReport(Guid.NewGuid(), "reporter-1", JobReportReason.SuspectedScam, "Details");
        report.Resolve("admin-1", "Notes");

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var command = new ResolveJobReportCommand(report.Id, DeactivationReason.Spam, "Details", null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage($"Cannot resolve report that is '{JobReportStatus.Resolved}'.");
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var jobId = Guid.NewGuid();
        var report = new JobReport(jobId, "reporter-1", JobReportReason.SuspectedScam, "Details");

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new ResolveJobReportCommand(report.Id, DeactivationReason.Spam, "Details", null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidPendingReport_ResolvesReport()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var jobId = Guid.NewGuid();
        var report = new JobReport(jobId, "reporter-1", JobReportReason.SuspectedScam, "Spam details");
        var job = (Job)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Job)); job.GetType().GetProperty("Title")?.SetValue(job, "Job Title"); job.GetType().GetProperty("EmployerId")?.SetValue(job, "employer-1");
        job.GetType().GetProperty("Id")?.SetValue(job, jobId);
        job.MarkAsPublished();

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var employer = new User("employer-1", "Employer Name", "employer@test.com", TalentX.Domain.Enums.UserRole.Employer);
        _userRepository.Setup(r => r.GetByUserIdAsync("employer-1"))
            .ReturnsAsync(employer);

        _emailService.Setup(s => s.RenderJobDeactivatedEmailAsync(It.IsAny<RenderJobDeactivatedEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>email</html>");

        var command = new ResolveJobReportCommand(
            report.Id,
            DeactivationReason.Spam,
            "Deactivation details",
            "Admin notes"
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Message.Should().Be("Job report resolved and job deactivated.");

        report.Status.Should().Be(JobReportStatus.Resolved);
        report.AdminNotes.Should().Be("Admin notes");
        report.ResolvedByUserId.Should().Be("admin-1");

        job.Status.Should().Be(JobStatus.Deactivated);
        job.DeactivationReason.Should().Be(DeactivationReason.Spam);
        job.DeactivationDetails.Should().Be("Deactivation details");

        _historyRepository.Verify(r => r.AddAsync(It.Is<JobModerationHistory>(h =>
            h.JobId == job.Id &&
            h.Action == JobModerationAction.Deactivated &&
            h.Reason == DeactivationReason.Spam &&
            h.Details == "Deactivation details"), It.IsAny<CancellationToken>()), Times.Once);

        _auditLogRepository.Verify(r => r.AddAsync(It.Is<AuditLog>(a =>
            a.Actor == "admin@test.com" &&
            a.Action.Contains("Resolved report") &&
            a.Action.Contains(job.Title)), It.IsAny<CancellationToken>()), Times.Once);

        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.Is<EmailDispatchLog>(log =>
            log.RecipientEmail == "employer@test.com" &&
            log.HtmlBody == "<html>email</html>"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _notificationService.Verify(s => s.PublishAsync(It.Is<PublishNotificationParameters>(p =>
            p.RecipientUserId == "employer-1" &&
            p.Type == NotificationType.JobReportResolved), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobReports.Commands.DismissJobReport;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobReports.Commands.DismissJobReport;

public class DismissJobReportCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ICurrentUserService> _currentUserService;
    private readonly DismissJobReportCommandHandler _handler;

    private readonly Mock<IJobReportRepository> _jobReportRepository;
    private readonly Mock<IJobRepository> _jobRepository;
    private readonly Mock<IAuditLogRepository> _auditLogRepository;

    public DismissJobReportCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUserService = new Mock<ICurrentUserService>();

        _jobReportRepository = new Mock<IJobReportRepository>();
        _jobRepository = new Mock<IJobRepository>();
        _auditLogRepository = new Mock<IAuditLogRepository>();

        _unitOfWork.SetupGet(u => u.JobReports).Returns(_jobReportRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.AuditLogs).Returns(_auditLogRepository.Object);

        _handler = new DismissJobReportCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Theory]
    [InlineData(null, "admin@test.com")]
    [InlineData("admin-1", null)]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException(string? userId, string? email)
    {
        _currentUserService.SetupGet(s => s.UserId).Returns(userId);
        _currentUserService.SetupGet(s => s.Email).Returns(email);

        var command = new DismissJobReportCommand(Guid.NewGuid(), null);

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

        var command = new DismissJobReportCommand(reportId, null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReportNotPending_ThrowsBadRequestException()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var report = new JobReport(Guid.NewGuid(), "reporter-1", JobReportReason.SuspectedScam, "Details");
        report.Dismiss("admin-1", "Notes");

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var command = new DismissJobReportCommand(report.Id, null);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage($"Cannot dismiss report that is '{JobReportStatus.Dismissed}'.");
    }

    [Fact]
    public async Task Handle_ValidState_DismissesReport()
    {
        _currentUserService.SetupGet(s => s.UserId).Returns("admin-1");
        _currentUserService.SetupGet(s => s.Email).Returns("admin@test.com");

        var jobId = Guid.NewGuid();
        var report = new JobReport(jobId, "reporter-1", JobReportReason.SuspectedScam, "Details");
        
        var job = (Job)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Job)); job.GetType().GetProperty("Title")?.SetValue(job, "Software Engineer"); job.GetType().GetProperty("EmployerId")?.SetValue(job, "employer-1");
        job.GetType().GetProperty("Id")?.SetValue(job, jobId);

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        
        _jobRepository.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new DismissJobReportCommand(report.Id, "Not spam.");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Message.Should().Be("Job report dismissed.");

        report.Status.Should().Be(JobReportStatus.Dismissed);
        report.AdminNotes.Should().Be("Not spam.");
        report.ResolvedByUserId.Should().Be("admin-1");

        _auditLogRepository.Verify(r => r.AddAsync(It.Is<AuditLog>(a => 
            a.Actor == "admin@test.com" && 
            a.Action == "Dismissed report for job 'Software Engineer'"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

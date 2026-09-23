using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobReports.Commands.ReportJob;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobReports.Commands.ReportJob;

public class ReportJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ReportJobCommandHandler>> _loggerMock;
    private readonly ReportJobCommandHandler _handler;

    public ReportJobCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork> { DefaultValue = DefaultValue.Mock };
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<ReportJobCommandHandler>>();

        _handler = new ReportJobCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedException()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns((string?)null);
        var command = new ReportJobCommand(Guid.NewGuid(), JobReportReason.SuspectedScam, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns("user1");
        _unitOfWorkMock.Setup(u => u.Jobs.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);
            
        var command = new ReportJobCommand(Guid.NewGuid(), JobReportReason.SuspectedScam, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_JobNotPublished_ThrowsBadRequestException()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns("user1");
        var job = Job.Create("emp1", "Title", "Company", "Desc", EmploymentType.FullTime, ExperienceLevel.MidLevel, "Loc", WorkArrangement.FullRemote, new List<string>(), 1, DateTime.UtcNow.AddDays(30));
        // Status is Draft by default
        _unitOfWorkMock.Setup(u => u.Jobs.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
            
        var command = new ReportJobCommand(job.Id, JobReportReason.SuspectedScam, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_DuplicateReport_ThrowsConflictException()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns("user1");
        var job = Job.Create("emp1", "Title", "Company", "Desc", EmploymentType.FullTime, ExperienceLevel.MidLevel, "Loc", WorkArrangement.FullRemote, new List<string>(), 1, DateTime.UtcNow.AddDays(30));
        SetJobStatus(job, JobStatus.Published);
        
        _unitOfWorkMock.Setup(u => u.Jobs.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _unitOfWorkMock.Setup(u => u.JobReports.ExistsForJobAndReporterAsync(job.Id, "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var command = new ReportJobCommand(job.Id, JobReportReason.SuspectedScam, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidState_CreatesReport()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns("user1");
        _currentUserServiceMock.Setup(c => c.Email).Returns("user1@test.com");

        var job = Job.Create("emp1", "Title", "Company", "Desc", EmploymentType.FullTime, ExperienceLevel.MidLevel, "Loc", WorkArrangement.FullRemote, new List<string>(), 1, DateTime.UtcNow.AddDays(30));
        SetJobStatus(job, JobStatus.Published);
        
        _unitOfWorkMock.Setup(u => u.Jobs.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        
        _unitOfWorkMock.Setup(u => u.JobReports.ExistsForJobAndReporterAsync(job.Id, "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
            
        _unitOfWorkMock.Setup(u => u.Users.GetAdminUserIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "admin1", "admin2" });
            
        var command = new ReportJobCommand(job.Id, JobReportReason.SuspectedScam, "More info");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.JobReports.AddAsync(It.Is<JobReport>(r => r.JobId == job.Id && r.ReporterUserId == "user1" && r.Reason == JobReportReason.SuspectedScam), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.AuditLogs.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        _notificationServiceMock.Verify(n => n.PublishAsync(It.Is<PublishNotificationParameters>(p => p.RecipientUserId == "admin1"), It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.PublishAsync(It.Is<PublishNotificationParameters>(p => p.RecipientUserId == "admin2"), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    private void SetJobStatus(Job job, JobStatus status)
    {
        var property = typeof(Job).GetProperty("Status");
        property?.SetValue(job, status);
    }
}

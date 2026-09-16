using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Services;

public sealed class JobAlertMatchingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<JobAlertMatchingService>> _logger = new();

    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<IJobAlertNotificationHistoryRepository> _historyRepository = new();
    private readonly Mock<ITalentNotificationPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository = new();

    private readonly JobAlertMatchingService _sut;

    public JobAlertMatchingServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.SetupGet(u => u.JobAlertNotificationHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(u => u.TalentNotificationPreferences).Returns(_preferenceRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);

        _notificationService.Setup(n => n.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDto(Guid.NewGuid(), NotificationType.JobAlertMatch, "title", "body", null, null, DateTime.UtcNow));

        _sut = new JobAlertMatchingService(_unitOfWork.Object, _emailService.Object, _notificationService.Object, _logger.Object);
    }

    private static Job CreatePublishedJob(
        string employerId = "employer-1",
        string title = "Senior Backend Engineer",
        string location = "Dhaka",
        EmploymentType employmentType = EmploymentType.FullTime,
        ExperienceLevel experienceLevel = ExperienceLevel.SeniorLevel,
        WorkArrangement workArrangement = WorkArrangement.FullRemote)
    {
        var job = Job.Create(
            employerId,
            title,
            "Acme Corp",
            "Build scalable systems.",
            employmentType,
            experienceLevel,
            location,
            workArrangement,
            ["C#", ".NET"],
            2,
            DateTime.UtcNow.AddDays(30));

        job.MarkAsPublished();
        return job;
    }

    private static JobAlert CreateAlert(
        string talentId = "talent-1",
        string name = "Backend Alert",
        string title = "Backend",
        string? location = "Dhaka",
        EmploymentType? employmentType = EmploymentType.FullTime,
        ExperienceLevel? experienceLevel = ExperienceLevel.SeniorLevel,
        WorkArrangement? workArrangement = WorkArrangement.FullRemote)
    {
        return JobAlert.Create(
            talentId,
            name,
            title,
            location,
            employmentType,
            experienceLevel,
            workArrangement);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_MatchesActiveAlert_EnqueuesEmailDispatchLogAndNotificationHistory()
    {
        var job = CreatePublishedJob();
        var alert = CreateAlert("talent-1");
        var talent = new Talent("talent-1", "John Doe", "john@example.com");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _preferenceRepository.Setup(r => r.GetByTalentIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _emailService.Setup(e => e.RenderJobAlertEmailAsync(It.IsAny<RenderJobAlertEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Alert email content</html>");

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(
            It.Is<JobAlertNotificationHistory>(h => h.TalentId == "talent-1" && h.JobId == job.Id && h.JobAlertId == alert.Id),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailDispatchLogRepository.Verify(r => r.AddAsync(
            It.Is<EmailDispatchLog>(l => l.RecipientEmail == "john@example.com" && l.RecipientName == "John Doe" && l.EmailType == "JobAlert"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _notificationService.Verify(n => n.PublishAsync(
            "talent-1",
            NotificationType.JobAlertMatch,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenCandidateHasJobAlertsEmailDisabled_SkipsCandidate()
    {
        var job = CreatePublishedJob();
        var alert = CreateAlert("talent-1");
        var preference = TalentNotificationPreference.CreateDefault("talent-1");
        preference.Update(false);

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _preferenceRepository.Setup(r => r.GetByTalentIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(It.IsAny<JobAlertNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(n => n.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenCandidateAlreadyNotified_SkipsCandidate()
    {
        var job = CreatePublishedJob();
        var alert = CreateAlert("talent-1");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "talent-1" });

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(It.IsAny<JobAlertNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(n => n.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenMultipleAlertsMatchSameCandidate_SendsOnlyOneEmail()
    {
        var job = CreatePublishedJob();
        var alert1 = CreateAlert("talent-1", name: "Alert 1", title: "Backend");
        var alert2 = CreateAlert("talent-1", name: "Alert 2", title: "Engineer");
        var talent = new Talent("talent-1", "John Doe", "john@example.com");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert1, alert2]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _preferenceRepository.Setup(r => r.GetByTalentIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _emailService.Setup(e => e.RenderJobAlertEmailAsync(It.IsAny<RenderJobAlertEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Alert email content</html>");

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(It.IsAny<JobAlertNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenAlertDoesNotMatchCriteria_SkipsAlert()
    {
        var job = CreatePublishedJob(title: "Backend Engineer", location: "Dhaka");
        var nonMatchingAlert = CreateAlert("talent-1", name: "Frontend Alert", title: "Frontend", location: "Tokyo");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([nonMatchingAlert]);

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(It.IsAny<JobAlertNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenAlertBelongsToPostingEmployer_SkipsAlert()
    {
        var job = CreatePublishedJob(employerId: "employer-123");
        var alert = CreateAlert(talentId: "employer-123", name: "My Own Alert");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(It.IsAny<JobAlertNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(It.IsAny<EmailDispatchLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenJobIsNotPublished_DoesNotProcessAlerts()
    {
        var draftJob = Job.Create(
            "employer-1",
            "Draft Job",
            "Acme",
            "Desc",
            EmploymentType.FullTime,
            ExperienceLevel.EntryLevel,
            "Remote",
            WorkArrangement.FullRemote,
            ["C#"],
            1,
            DateTime.UtcNow.AddDays(10));

        _jobRepository.Setup(r => r.GetByIdAsync(draftJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftJob);

        await _sut.ProcessAlertsForJobAsync(draftJob.Id, CancellationToken.None);

        _jobAlertRepository.Verify(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenTalentRecordNotFound_FallsBackToUser()
    {
        var job = CreatePublishedJob();
        var alert = CreateAlert("talent-1");
        var user = new User("talent-1", "Jane User", "jane@example.com", UserRole.Talent);

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _preferenceRepository.Setup(r => r.GetByTalentIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);
        _userRepository.Setup(r => r.GetByUserIdAsync("talent-1"))
            .ReturnsAsync(user);
        _emailService.Setup(e => e.RenderJobAlertEmailAsync(It.IsAny<RenderJobAlertEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Alert email content</html>");

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _emailDispatchLogRepository.Verify(r => r.AddAsync(
            It.Is<EmailDispatchLog>(l => l.RecipientEmail == "jane@example.com" && l.RecipientName == "Jane User"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAlertsForJobAsync_WhenNotificationPublishThrows_StillWritesHistoryAndEmailAndProcessesNextTalent()
    {
        var job = CreatePublishedJob();
        var alertForTalentOne = CreateAlert("talent-1", name: "Alert 1");
        var alertForTalentTwo = CreateAlert("talent-2", name: "Alert 2");
        var talentOne = new Talent("talent-1", "John Doe", "john@example.com");
        var talentTwo = new Talent("talent-2", "Jane Smith", "jane.smith@example.com");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobAlertRepository.Setup(r => r.GetAllActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alertForTalentOne, alertForTalentTwo]);
        _historyRepository.Setup(r => r.GetNotifiedTalentIdsAsync(job.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _preferenceRepository.Setup(r => r.GetByTalentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talentOne);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talentTwo);
        _emailService.Setup(e => e.RenderJobAlertEmailAsync(It.IsAny<RenderJobAlertEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Alert email content</html>");

        _notificationService.Setup(n => n.PublishAsync(
                "talent-1",
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated notification publish failure."));

        await _sut.ProcessAlertsForJobAsync(job.Id, CancellationToken.None);

        _historyRepository.Verify(r => r.AddAsync(
            It.Is<JobAlertNotificationHistory>(h => h.TalentId == "talent-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _historyRepository.Verify(r => r.AddAsync(
            It.Is<JobAlertNotificationHistory>(h => h.TalentId == "talent-2"),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailDispatchLogRepository.Verify(r => r.AddAsync(
            It.Is<EmailDispatchLog>(l => l.RecipientEmail == "john@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailDispatchLogRepository.Verify(r => r.AddAsync(
            It.Is<EmailDispatchLog>(l => l.RecipientEmail == "jane.smith@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);

        _notificationService.Verify(n => n.PublishAsync(
            "talent-2",
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

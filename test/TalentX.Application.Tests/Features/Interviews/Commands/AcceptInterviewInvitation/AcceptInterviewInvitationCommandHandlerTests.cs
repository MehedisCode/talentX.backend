using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews.Commands.AcceptInterviewInvitation;

public class AcceptInterviewInvitationCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IJobApplicationStatusHistoryRepository> _historyRepository = new();
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<AcceptInterviewInvitationCommandHandler>> _logger = new();
    private readonly AcceptInterviewInvitationCommandHandler _handler;

    public AcceptInterviewInvitationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_interviewInvitationRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplicationStatusHistories).Returns(_historyRepository.Object);
        _unitOfWork.SetupGet(u => u.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);
        _currentUserService.SetupGet(u => u.Email).Returns((string?)null);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        _handler = new AcceptInterviewInvitationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object,
            _notificationService.Object,
            _logger.Object);
    }

    private static InterviewInvitation CreateOnlineAssessmentInvitation(DateTime? assessmentDeadline)
    {
        return new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            TalentUserId,
            InterviewType.OnlineAssessment,
            assessmentUrl: "https://assessment.example.com",
            assessmentDeadline: assessmentDeadline);
    }

    private static InterviewInvitation CreateOnCallInvitationWithTimeslot(out Guid timeslotId, DateTime timeslotStart)
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            TalentUserId,
            InterviewType.OnCall,
            locationOrLink: "https://meet.example.com/room");

        invitation.AddTimeslot(timeslotStart, timeslotStart.AddMinutes(30));
        timeslotId = invitation.Timeslots[0].Id;
        return invitation;
    }

    [Fact]
    public async Task Handle_OnlineAssessment_WhenDeadlineInFuture_Accepts()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new AcceptInterviewInvitationCommand(invitation.Id);

        await _handler.Handle(command, CancellationToken.None);

        invitation.Status.Should().Be(InterviewInvitationStatus.Accepted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnlineAssessment_WhenDeadlineAlreadyPassed_ThrowsBadRequest()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(-1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new AcceptInterviewInvitationCommand(invitation.Id);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        invitation.Status.Should().Be(InterviewInvitationStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnCall_WhenSelectedTimeslotInFuture_Accepts()
    {
        var invitation = CreateOnCallInvitationWithTimeslot(out var timeslotId, DateTime.UtcNow.AddHours(1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new AcceptInterviewInvitationCommand(invitation.Id, timeslotId);

        await _handler.Handle(command, CancellationToken.None);

        invitation.Status.Should().Be(InterviewInvitationStatus.Accepted);
    }

    [Fact]
    public async Task Handle_OnCall_WhenEveryTimeslotHasPassed_ThrowsBadRequest()
    {
        var invitation = CreateOnCallInvitationWithTimeslot(out var timeslotId, DateTime.UtcNow.AddHours(-1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new AcceptInterviewInvitationCommand(invitation.Id, timeslotId);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        invitation.Status.Should().Be(InterviewInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenInvitationAccepted_PublishesInterviewInvitationAcceptedNotification()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));
        var job = CreateJob(invitation.EmployerUserId);
        var application = new JobApplication(
            invitation.JobId,
            TalentUserId,
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf");

        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _jobRepository
            .Setup(r => r.GetByIdAsync(invitation.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(invitation.JobApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var command = new AcceptInterviewInvitationCommand(invitation.Id);

        await _handler.Handle(command, CancellationToken.None);

        _notificationService.Verify(
            service => service.PublishAsync(
                It.Is<PublishNotificationParameters>(parameters =>
                    parameters.RecipientUserId == invitation.EmployerUserId &&
                    parameters.Type == NotificationType.InterviewInvitationAccepted &&
                    parameters.Title == "Interview Accepted" &&
                    parameters.Body == "Jane Doe accepted the interview invitation for Senior Backend Engineer." &&
                    parameters.Metadata!.Contains(invitation.Id.ToString()) &&
                    parameters.Metadata.Contains("Jane Doe")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTalentRenamedAfterApply_GreetsWithCurrentName()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));
        var job = CreateJob(invitation.EmployerUserId);
        var application = new JobApplication(
            invitation.JobId,
            TalentUserId,
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf");
        var renamedTalent = new Talent(TalentUserId, "Jane Renamed", "jane@example.com");

        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _jobRepository
            .Setup(r => r.GetByIdAsync(invitation.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(invitation.JobApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renamedTalent);

        await _handler.Handle(new AcceptInterviewInvitationCommand(invitation.Id), CancellationToken.None);

        invitation.Status.Should().Be(InterviewInvitationStatus.Accepted);
        _emailService.Verify(
            service => service.RenderInterviewConfirmationEmailAsync(
                "Jane Renamed",
                job.Title,
                job.CompanyName,
                invitation.Type.ToString(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _notificationService.Verify(
            service => service.PublishAsync(
                It.Is<PublishNotificationParameters>(parameters =>
                    parameters.Body == "Jane Renamed accepted the interview invitation for Senior Backend Engineer."),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Job CreateJob(string employerId)
    {
        return Job.Create(
            employerId,
            "Senior Backend Engineer",
            "Acme Corp",
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { ".NET" },
            1,
            DateTime.UtcNow.AddDays(30));
    }
}

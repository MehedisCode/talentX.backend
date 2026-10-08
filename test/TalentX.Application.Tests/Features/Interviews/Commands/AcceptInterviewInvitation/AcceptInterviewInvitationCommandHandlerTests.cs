using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews.Commands.AcceptInterviewInvitation;

public class AcceptInterviewInvitationCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly AcceptInterviewInvitationCommandHandler _handler;

    public AcceptInterviewInvitationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_interviewInvitationRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);
        _currentUserService.SetupGet(u => u.Email).Returns((string?)null);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        _handler = new AcceptInterviewInvitationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object);
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

    [Theory]
    [InlineData(InterviewInvitationStatus.Accepted)]
    [InlineData(InterviewInvitationStatus.Declined)]
    [InlineData(InterviewInvitationStatus.Cancelled)]
    public async Task Handle_WhenInvitationIsNotPending_ThrowsConflict(InterviewInvitationStatus existingStatus)
    {
        var invitation = CreateOnCallInvitationWithTimeslot(out var timeslotId, DateTime.UtcNow.AddHours(1));
        switch (existingStatus)
        {
            case InterviewInvitationStatus.Accepted:
                invitation.Accept(timeslotId, DateTime.UtcNow.AddHours(1));
                break;
            case InterviewInvitationStatus.Declined:
                invitation.Decline();
                break;
            case InterviewInvitationStatus.Cancelled:
                invitation.Cancel();
                break;
        }

        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new AcceptInterviewInvitationCommand(invitation.Id, timeslotId);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        invitation.Status.Should().Be(existingStatus);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Interviews.Commands.DeclineInterviewInvitation;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews.Commands.DeclineInterviewInvitation;

public class DeclineInterviewInvitationCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeclineInterviewInvitationCommandHandler _handler;

    public DeclineInterviewInvitationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_interviewInvitationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new DeclineInterviewInvitationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
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

    private static InterviewInvitation CreateOnCallInvitationWithTimeslot(DateTime timeslotStart)
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            TalentUserId,
            InterviewType.OnCall,
            locationOrLink: "https://meet.example.com/room");

        invitation.AddTimeslot(timeslotStart, timeslotStart.AddMinutes(30));
        return invitation;
    }

    [Fact]
    public async Task Handle_OnlineAssessment_WhenDeadlineInFuture_Declines()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new DeclineInterviewInvitationCommand(invitation.Id, "Found another role");

        await _handler.Handle(command, CancellationToken.None);

        invitation.Status.Should().Be(InterviewInvitationStatus.Declined);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnlineAssessment_WhenDeadlineAlreadyPassed_ThrowsBadRequest()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(-1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new DeclineInterviewInvitationCommand(invitation.Id);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        invitation.Status.Should().Be(InterviewInvitationStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnCall_WhenAtLeastOneTimeslotStillInFuture_Declines()
    {
        var invitation = CreateOnCallInvitationWithTimeslot(DateTime.UtcNow.AddHours(1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new DeclineInterviewInvitationCommand(invitation.Id);

        await _handler.Handle(command, CancellationToken.None);

        invitation.Status.Should().Be(InterviewInvitationStatus.Declined);
    }

    [Fact]
    public async Task Handle_OnCall_WhenEveryTimeslotHasPassed_ThrowsBadRequest()
    {
        var invitation = CreateOnCallInvitationWithTimeslot(DateTime.UtcNow.AddHours(-1));
        _interviewInvitationRepository
            .Setup(r => r.GetByIdWithTimeslotsAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var command = new DeclineInterviewInvitationCommand(invitation.Id);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        invitation.Status.Should().Be(InterviewInvitationStatus.Pending);
    }
}

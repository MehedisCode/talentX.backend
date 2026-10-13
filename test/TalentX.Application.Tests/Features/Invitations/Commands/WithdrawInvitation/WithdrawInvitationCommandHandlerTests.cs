using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Invitations.Commands.WithdrawInvitation;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Invitations.Commands.WithdrawInvitation;

public class WithdrawInvitationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IInvitationRepository> _invitationRepo = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly WithdrawInvitationCommandHandler _handler;

    private const string CurrentEmployerId = "employer-1";
    private const string TalentUserId = "talent-42";
    private readonly Guid _jobId = Guid.NewGuid();
    private readonly Guid _invitationId = Guid.NewGuid();

    public WithdrawInvitationCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentEmployerId);

        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.Invitations).Returns(_invitationRepo.Object);

        _handler = new WithdrawInvitationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    private Job CreateOwnedJob()
    {
        return Job.Create(
            CurrentEmployerId, "Backend Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 2,
            DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_WhenInvitationPending_WithdrawsAndReturnsDto()
    {
        var job = CreateOwnedJob();
        var invitation = new Invitation(_jobId, TalentUserId);

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _invitationRepo.Setup(r => r.GetByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _talentRepo.Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent(TalentUserId, "Bob", "bob@example.com"));

        var result = await _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(InvitationStatus.Withdrawn);
        result.IsActive.Should().BeFalse();

        _unitOfWork.Verify(u => u.Invitations.Update(invitation), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvitationAccepted_ThrowsConflictException()
    {
        var job = CreateOwnedJob();
        var invitation = new Invitation(_jobId, TalentUserId);
        invitation.Accept();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _invitationRepo.Setup(r => r.GetByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Only pending invitations can be withdrawn*");

        _unitOfWork.Verify(u => u.Invitations.Update(It.IsAny<Invitation>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvitationDeclined_ThrowsConflictException()
    {
        var job = CreateOwnedJob();
        var invitation = new Invitation(_jobId, TalentUserId);
        invitation.Decline();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _invitationRepo.Setup(r => r.GetByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Only pending invitations can be withdrawn*");
    }

    [Fact]
    public async Task Handle_WhenJobBelongsToAnotherEmployer_ThrowsForbiddenException()
    {
        var job = Job.Create(
            "other-employer", "Backend Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 2,
            DateTime.UtcNow.AddDays(30));

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenInvitationBelongsToAnotherJob_ThrowsBadRequestException()
    {
        var job = CreateOwnedJob();
        var invitation = new Invitation(Guid.NewGuid(), TalentUserId);

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _invitationRepo.Setup(r => r.GetByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*does not belong to the specified job*");
    }

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ThrowsNotFoundException()
    {
        var job = CreateOwnedJob();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _invitationRepo.Setup(r => r.GetByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invitation?)null);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
    {
        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(
            new WithdrawInvitationCommand(_jobId, _invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

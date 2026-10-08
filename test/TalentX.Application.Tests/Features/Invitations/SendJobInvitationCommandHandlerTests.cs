using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Invitations.Commands.SendJobInvitation;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Tests.Features.Invitations;

public class SendJobInvitationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepo = new();
    private readonly Mock<ITalentRepository> _talentRepo = new();
    private readonly Mock<IInvitationRepository> _invitationRepo = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<SendJobInvitationCommandHandler>> _logger = new();
    private readonly SendJobInvitationCommandHandler _handler;

    private const string CurrentEmployerId = "employer-1";
    private readonly Guid _jobId = Guid.NewGuid();
    private const string TalentUserId = "talent-42";

    public SendJobInvitationCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns(CurrentEmployerId);

        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepo.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepo.Object);
        _unitOfWork.SetupGet(u => u.Invitations).Returns(_invitationRepo.Object);

        _handler = new SendJobInvitationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object,
            _notificationService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesInvitationSuccessfully()
    {
        var job = Job.Create(
            CurrentEmployerId, "Backend Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent(TalentUserId, "Bob", "bob@example.com");
        _talentRepo.Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        _invitationRepo.Setup(r => r.ExistsActiveAsync(_jobId, TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Invitation? createdInvitation = null;
        _invitationRepo.Setup(r => r.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()))
            .Callback<Invitation, CancellationToken>((inv, _) => createdInvitation = inv)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new SendJobInvitationCommand(_jobId, TalentUserId), CancellationToken.None);

        result.Should().NotBeNull();
        result.JobId.Should().Be(job.Id);
        result.TalentUserId.Should().Be(TalentUserId);
        result.IsActive.Should().BeTrue();

        createdInvitation.Should().NotBeNull();
        createdInvitation!.JobId.Should().Be(job.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _notificationService.Verify(
            s => s.PublishAsync(
                It.Is<PublishNotificationParameters>(p =>
                    p.RecipientUserId == TalentUserId &&
                    p.Type == NotificationType.JobInvitationReceived &&
                    p.Title == "Job Invitation" &&
                    p.Body.Contains("Acme") &&
                    p.Metadata != null &&
                    p.Metadata.Contains(job.Id.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotificationPublishThrows_StillReturnsSuccess()
    {
        var job = Job.Create(
            CurrentEmployerId, "Backend Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent(TalentUserId, "Bob", "bob@example.com");
        _talentRepo.Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        _invitationRepo.Setup(r => r.ExistsActiveAsync(_jobId, TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _notificationService
            .Setup(s => s.PublishAsync(It.IsAny<PublishNotificationParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connection down"));

        var result = await _handler.Handle(new SendJobInvitationCommand(_jobId, TalentUserId), CancellationToken.None);

        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenJobIsExpired_CreatesInvitation()
    {
        var job = Job.Create(
            CurrentEmployerId, "Backend Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        job.MarkAsExpired();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent(TalentUserId, "Bob", "bob@example.com");
        _talentRepo.Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        _invitationRepo.Setup(r => r.ExistsActiveAsync(_jobId, TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new SendJobInvitationCommand(_jobId, TalentUserId), CancellationToken.None);

        result.Should().NotBeNull();
        result.JobId.Should().Be(job.Id);
        result.IsActive.Should().BeTrue();

        _invitationRepo.Verify(r => r.AddAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyInvited_ThrowsConflictException()
    {
        var job = Job.Create(
            CurrentEmployerId, "Engineer", "Acme", "Desc",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, "Remote",
            WorkArrangement.FullRemote, new List<string> { "C#" }, 1,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();

        _jobRepo.Setup(r => r.GetByIdAsync(_jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var talent = new Talent(TalentUserId, "Bob", "bob@example.com");
        _talentRepo.Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        _invitationRepo.Setup(r => r.ExistsActiveAsync(_jobId, TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new SendJobInvitationCommand(_jobId, TalentUserId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*active invitation has already been sent*");
    }
}

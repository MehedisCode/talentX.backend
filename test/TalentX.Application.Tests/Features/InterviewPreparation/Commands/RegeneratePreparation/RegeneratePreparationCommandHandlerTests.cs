using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.RegeneratePreparation;

public class RegeneratePreparationCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewInvitationRepository> _invitationRepository = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IInterviewPreparationAiService> _aiService = new();
    private readonly Mock<ILogger<RegeneratePreparationCommandHandler>> _logger = new();
    private readonly RegeneratePreparationCommandHandler _handler;

    public RegeneratePreparationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_invitationRepository.Object);
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new RegeneratePreparationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _aiService.Object,
            Options.Create(new InterviewPreparationSettings()),
            _logger.Object);
    }

    private static InterviewInvitation CreateAcceptedInvitation(Guid jobId)
    {
        var invitation = new InterviewInvitation(
            jobId, Guid.NewGuid(), "employer-1", TalentUserId, InterviewType.OnCall);
        invitation.Accept(selectedTimeslotId: null, scheduledAt: DateTime.UtcNow.AddHours(2));
        return invitation;
    }

    private static Job CreateJob()
    {
        return Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme",
            "Build scalable systems",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static InterviewPreparationEntity CreatePreparation(
        Guid invitationId, DateTime? scheduledAt = null, string? talentUserId = null)
    {
        return InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: invitationId,
                TalentUserId: talentUserId ?? TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: """[{"question":"Old question","category":"Technical","tip":null}]""",
                BehavioralQuestions: "[]",
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: """["Old topic"]""",
                SuggestedRevisionAreas: "[]"));
    }

    private static InterviewPreparationResult AiResult()
    {
        return new InterviewPreparationResult
        {
            TechnicalQuestions = """[{"question":"New question","category":"Technical","tip":"Tip"}]""",
            BehavioralQuestions = "[]",
            ExperienceBasedQuestions = "[]",
            RoleSpecificQuestions = "[]",
            KeyTopicsToPrepare = """["New topic"]""",
            SuggestedRevisionAreas = """["New area"]""",
        };
    }

    private void SetUpFlow(
        InterviewPreparationEntity existing,
        InterviewInvitation? invitation = null,
        int todayCount = 0)
    {
        invitation ??= CreateAcceptedInvitation(existing.JobId);

        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _preparationRepository
            .Setup(r => r.CountTodayByTalentAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todayCount);
        _invitationRepository
            .Setup(r => r.GetByIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _jobRepository
            .Setup(r => r.GetByIdAsync(invitation.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJob());
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent(TalentUserId, "Ada Lovelace", "ada@example.com"));
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());
        _aiService
            .Setup(s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiResult());
    }

    [Fact]
    public async Task Handle_WhenRegenerationIsAllowed_UpdatesPreparationAndBumpsGenerationCount()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        SetUpFlow(existing);

        var result = await _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        result.GenerationCount.Should().Be(2);
        result.Status.Should().Be("Regenerated");
        result.TechnicalQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("New question");
        result.KeyTopicsToPrepare.Should().Equal("New topic");
        result.AdditionalQuestions.Should().BeEmpty();

        _preparationRepository.Verify(r => r.Update(existing), Times.Once);
        _preparationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewPreparationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClearsPreviouslyGeneratedAdditionalQuestions()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        existing.SetAdditionalQuestions("""[{"question":"Extra","category":"Additional","tip":null}]""");
        SetUpFlow(existing);

        await _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        existing.AdditionalQuestions.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPreparationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewPreparationEntity?)null);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview preparation not found.");
    }

    [Fact]
    public async Task Handle_WhenPreparationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var existing = CreatePreparation(Guid.NewGuid(), talentUserId: "talent-2");
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview preparation.");
        _aiService.Verify(
            s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInterviewAlreadyTookPlace_ThrowsForbiddenException()
    {
        var existing = CreatePreparation(
            Guid.NewGuid(), scheduledAt: DateTime.UtcNow.AddHours(-2));
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Preparation is no longer accessible as the interview has already taken place.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegenerationLimitIsReached_ThrowsBadRequestException()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        existing.Regenerate(new RegenerateInterviewPreparationPayload("[]", "[]", "[]", "[]", "[]", "[]"));
        existing.Regenerate(new RegenerateInterviewPreparationPayload("[]", "[]", "[]", "[]", "[]", "[]"));
        existing.GenerationCount.Should().Be(3);
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Maximum regeneration limit reached (3 times).");
        _aiService.Verify(
            s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDailyGenerationLimitIsReached_ThrowsBadRequestException()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        SetUpFlow(existing, todayCount: 10);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Daily interview preparation generation limit reached.");
    }

    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_ThrowsNotFoundException()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        SetUpFlow(existing);
        _invitationRepository
            .Setup(r => r.GetByIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewInvitation?)null);

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview invitation not found.");
    }

    [Fact]
    public async Task Handle_WhenAiServiceFails_ThrowsBadGatewayAndDoesNotPersist()
    {
        var existing = CreatePreparation(Guid.NewGuid());
        SetUpFlow(existing);
        _aiService
            .Setup(s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gemini API error"));

        var act = () => _handler.Handle(
            new RegeneratePreparationCommand(existing.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("AI service is temporarily unavailable.");
        _preparationRepository.Verify(r => r.Update(It.IsAny<InterviewPreparationEntity>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public class GenerateAdditionalQuestionsCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IInterviewPreparationAiService> _aiService = new();
    private readonly Mock<ILogger<GenerateAdditionalQuestionsCommandHandler>> _logger = new();
    private readonly GenerateAdditionalQuestionsCommandHandler _handler;

    public GenerateAdditionalQuestionsCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new GenerateAdditionalQuestionsCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _aiService.Object,
            _logger.Object);
    }

    private static InterviewPreparationEntity CreatePreparation(
        DateTime? scheduledAt = null, string? talentUserId = null)
    {
        return InterviewPreparationEntity.Create(
            interviewInvitationId: Guid.NewGuid(),
            talentUserId: talentUserId ?? TalentUserId,
            jobId: Guid.NewGuid(),
            jobTitle: "Backend Engineer",
            companyName: "Acme",
            interviewType: "OnCall",
            scheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
            technicalQuestions: """[{"question":"Existing technical","category":"Technical","tip":null}]""",
            behavioralQuestions: """[{"question":"Existing behavioral","category":"Behavioral","tip":null}]""",
            experienceBasedQuestions: "[]",
            roleSpecificQuestions: """[{"question":"Existing role","category":"Role-Specific","tip":null}]""",
            keyTopicsToPrepare: "[]",
            suggestedRevisionAreas: "[]");
    }

    private static AdditionalQuestionsResult AiResult()
    {
        return new AdditionalQuestionsResult
        {
            AdditionalQuestions = """[{"question":"Fresh question","category":"Technical","tip":"Tip"}]""",
        };
    }

    private void SetUpFlow(InterviewPreparationEntity existing, bool talentExists = true)
    {
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talentExists
                ? new Talent(TalentUserId, "Ada Lovelace", "ada@example.com")
                : null);
        _aiService
            .Setup(s => s.GenerateAdditionalQuestionsAsync(
                It.IsAny<AdditionalQuestionsContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiResult());
    }

    [Fact]
    public async Task Handle_WhenGenerationIsAllowed_SetsAdditionalQuestionsAndSaves()
    {
        var existing = CreatePreparation();
        SetUpFlow(existing);

        var result = await _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        result.AdditionalQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("Fresh question");
        result.GenerationCount.Should().Be(1);
        existing.AdditionalQuestions.Should().Contain("Fresh question");

        _preparationRepository.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesExistingQuestionsSoAiDoesNotDuplicateThem()
    {
        var existing = CreatePreparation();
        SetUpFlow(existing);

        await _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        _aiService.Verify(
            s => s.GenerateAdditionalQuestionsAsync(
                It.Is<AdditionalQuestionsContext>(c =>
                    c.JobTitle == "Backend Engineer" &&
                    c.CompanyName == "Acme" &&
                    c.TalentName == "Ada Lovelace" &&
                    c.ExistingTechnicalQuestions.Contains("Existing technical") &&
                    c.ExistingBehavioralQuestions.Contains("Existing behavioral") &&
                    c.ExistingRoleSpecificQuestions.Contains("Existing role")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTalentProfileIsMissing_FallsBackToCandidateName()
    {
        var existing = CreatePreparation();
        SetUpFlow(existing, talentExists: false);

        await _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        _aiService.Verify(
            s => s.GenerateAdditionalQuestionsAsync(
                It.Is<AdditionalQuestionsContext>(c => c.TalentName == "Candidate"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreparationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewPreparationEntity?)null);

        var act = () => _handler.Handle(
            new GenerateAdditionalQuestionsCommand(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview preparation not found.");
    }

    [Fact]
    public async Task Handle_WhenPreparationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var existing = CreatePreparation(talentUserId: "talent-2");
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview preparation.");
        _aiService.Verify(
            s => s.GenerateAdditionalQuestionsAsync(
                It.IsAny<AdditionalQuestionsContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInterviewAlreadyTookPlace_ThrowsForbiddenException()
    {
        var existing = CreatePreparation(scheduledAt: DateTime.UtcNow.AddHours(-2));
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(existing.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Preparation is no longer accessible as the interview has already taken place.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAiServiceFails_ThrowsBadGatewayAndDoesNotPersist()
    {
        var existing = CreatePreparation();
        SetUpFlow(existing);
        _aiService
            .Setup(s => s.GenerateAdditionalQuestionsAsync(
                It.IsAny<AdditionalQuestionsContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gemini API error"));

        var act = () => _handler.Handle(
            new GenerateAdditionalQuestionsCommand(existing.InterviewInvitationId),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("AI service is temporarily unavailable.");
        _preparationRepository.Verify(r => r.Update(It.IsAny<InterviewPreparationEntity>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

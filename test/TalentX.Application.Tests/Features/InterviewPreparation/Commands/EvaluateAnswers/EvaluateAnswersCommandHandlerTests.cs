using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.EvaluateAnswers;

public class EvaluateAnswersCommandHandlerTests
{
    private const string TalentUserId = "talent-1";
    private const string QuestionJson =
        """[{"question":"What is a closure?","category":"Technical","tip":null}]""";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IInterviewPreparationAiService> _aiService = new();
    private readonly Mock<ILogger<EvaluateAnswersCommandHandler>> _logger = new();
    private readonly EvaluateAnswersCommandHandler _handler;

    public EvaluateAnswersCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent(TalentUserId, "Ada Lovelace", "ada@example.com"));

        _handler = new EvaluateAnswersCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _aiService.Object,
            Options.Create(new InterviewPreparationSettings()),
            _logger.Object);
    }

    private static InterviewPreparationEntity CreatePreparation(
        DateTime? scheduledAt = null, string? talentUserId = null)
    {
        return InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: talentUserId ?? TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: QuestionJson,
                BehavioralQuestions: "[]",
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: "[]",
                SuggestedRevisionAreas: "[]"));
    }

    private static PreparationAnswer CreateAnswer(Guid preparationId, string answerText)
    {
        return PreparationAnswer.Create(new CreatePreparationAnswerPayload(
            preparationId, "Technical", 0, "What is a closure?", answerText, true));
    }

    private static AnswersEvaluationResult AiResult()
    {
        return new AnswersEvaluationResult
        {
            OverallScore = 75,
            Summary = "Solid answers with room for more structure.",
            Strengths = """["Concrete examples"]""",
            AreasToImprove = """["Add measurable outcomes"]""",
            QuestionFeedback =
                """[{"category":"Technical","questionIndex":0,"score":72,"feedback":"Good.","suggestedAnswer":""}]"""
        };
    }

    private void SetUpPreparation(
        InterviewPreparationEntity preparation,
        IReadOnlyList<PreparationAnswer> answers,
        InterviewPreparationEvaluation? evaluation = null)
    {
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(
                preparation.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        _preparationRepository
            .Setup(r => r.GetAnswersByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answers);
        _preparationRepository
            .Setup(r => r.GetEvaluationByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluation);
        _aiService
            .Setup(s => s.EvaluateAnswersAsync(
                It.IsAny<AnswersEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiResult());
    }

    [Fact]
    public async Task Handle_WhenAnswersExist_EvaluatesAndPersistsEvaluation()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation, new List<PreparationAnswer>
        {
            CreateAnswer(preparation.Id, "A closure captures its lexical scope.")
        });

        var result = await _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        _aiService.Verify(
            s => s.EvaluateAnswersAsync(
                It.Is<AnswersEvaluationContext>(c =>
                    c.JobTitle == "Backend Engineer"
                    && c.TalentName == "Ada Lovelace"
                    && c.TotalQuestionCount == 1
                    && c.AnsweredQuestions.Count == 1
                    && c.AnsweredQuestions[0].Answer == "A closure captures its lexical scope."),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _preparationRepository.Verify(
            r => r.AddEvaluationAsync(
                It.Is<InterviewPreparationEvaluation>(e =>
                    e.InterviewPreparationId == preparation.Id
                    && e.OverallScore == 75
                    && e.AnsweredCount == 1
                    && e.TotalCount == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Evaluation.Should().NotBeNull();
        result.Evaluation!.OverallScore.Should().Be(75);
        result.Evaluation.Strengths.Should().Equal("Concrete examples");
        result.Evaluation.QuestionFeedback.Should().ContainSingle()
            .Which.Score.Should().Be(72);
    }

    [Fact]
    public async Task Handle_WhenEvaluationAlreadyExists_ReplacesIt()
    {
        var preparation = CreatePreparation();
        var existing = InterviewPreparationEvaluation.Create(
            new CreateEvaluationPayload(preparation.Id, 40, "Old", "[]", "[]", "[]", 1, 1));
        SetUpPreparation(
            preparation,
            new List<PreparationAnswer> { CreateAnswer(preparation.Id, "My answer") },
            existing);

        await _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        existing.OverallScore.Should().Be(75);
        existing.AnsweredCount.Should().Be(1);
        _preparationRepository.Verify(r => r.UpdateEvaluation(existing), Times.Once);
        _preparationRepository.Verify(
            r => r.AddEvaluationAsync(It.IsAny<InterviewPreparationEvaluation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoQuestionWasAnswered_ThrowsBadRequestException()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation, new List<PreparationAnswer>
        {
            CreateAnswer(preparation.Id, answerText: "   ")
        });

        var act = () => _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Answer at least one question before requesting an AI evaluation.");
        _aiService.Verify(
            s => s.EvaluateAnswersAsync(
                It.IsAny<AnswersEvaluationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPreparationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewPreparationEntity?)null);

        var act = () => _handler.Handle(
            new EvaluateAnswersCommand(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview preparation not found.");
    }

    [Fact]
    public async Task Handle_WhenPreparationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var preparation = CreatePreparation(talentUserId: "talent-2");
        SetUpPreparation(preparation, new List<PreparationAnswer>
        {
            CreateAnswer(preparation.Id, "My answer")
        });

        var act = () => _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview preparation.");
        _aiService.Verify(
            s => s.EvaluateAnswersAsync(
                It.IsAny<AnswersEvaluationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInterviewAlreadyTookPlace_ThrowsForbiddenException()
    {
        var preparation = CreatePreparation(scheduledAt: DateTime.UtcNow.AddHours(-2));
        SetUpPreparation(preparation, new List<PreparationAnswer>
        {
            CreateAnswer(preparation.Id, "My answer")
        });

        var act = () => _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Preparation is no longer accessible as the interview has already taken place.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAiServiceFails_ThrowsBadGatewayAndDoesNotPersist()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation, new List<PreparationAnswer>
        {
            CreateAnswer(preparation.Id, "My answer")
        });
        _aiService
            .Setup(s => s.EvaluateAnswersAsync(
                It.IsAny<AnswersEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gemini API error"));

        var act = () => _handler.Handle(
            new EvaluateAnswersCommand(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("AI service is temporarily unavailable.");
        _preparationRepository.Verify(
            r => r.AddEvaluationAsync(It.IsAny<InterviewPreparationEvaluation>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

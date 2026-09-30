using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.SaveAnswer;

public class SaveAnswerCommandHandlerTests
{
    private const string TalentUserId = "talent-1";
    private const string QuestionJson =
        """[{"question":"What is a closure?","category":"Technical","tip":"Keep it concise"}]""";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly SaveAnswerCommandHandler _handler;

    public SaveAnswerCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);

        _handler = new SaveAnswerCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            Options.Create(new InterviewPreparationSettings()));
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

    private static PreparationAnswer CreateAnswer(
        Guid preparationId, string answerText, bool practiced = false)
    {
        return PreparationAnswer.Create(new CreatePreparationAnswerPayload(
            preparationId, "Technical", 0, "What is a closure?", answerText, practiced));
    }

    private void SetUpPreparation(
        InterviewPreparationEntity preparation,
        IReadOnlyList<PreparationAnswer>? answers = null,
        InterviewPreparationEvaluation? evaluation = null)
    {
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(
                preparation.InterviewInvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        _preparationRepository
            .Setup(r => r.GetAnswerAsync(
                preparation.Id, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PreparationAnswer?)null);
        _preparationRepository
            .Setup(r => r.GetAnswersByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answers ?? new List<PreparationAnswer>());
        _preparationRepository
            .Setup(r => r.GetEvaluationByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluation);
    }

    private static SaveAnswerCommand Command(
        Guid invitationId, string answer = "A closure is a function over its lexical scope.")
    {
        return new SaveAnswerCommand(
            invitationId, "Technical", 0, "Client supplied question text", answer, Practiced: true);
    }

    [Fact]
    public async Task Handle_WhenNoAnswerExistsYet_AddsAnswerUsingStoredQuestionText()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation);

        await _handler.Handle(
            Command(preparation.InterviewInvitationId), CancellationToken.None);

        _preparationRepository.Verify(
            r => r.AddAnswerAsync(
                It.Is<PreparationAnswer>(a =>
                    a.InterviewPreparationId == preparation.Id
                    && a.Category == "Technical"
                    && a.QuestionIndex == 0
                    && a.QuestionText == "What is a closure?"
                    && a.AnswerText == "A closure is a function over its lexical scope."
                    && a.IsPracticed),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAnswerAlreadyExists_UpdatesItInsteadOfAdding()
    {
        var preparation = CreatePreparation();
        var existing = CreateAnswer(preparation.Id, answerText: "Old answer");
        SetUpPreparation(preparation, answers: new List<PreparationAnswer> { existing });
        _preparationRepository
            .Setup(r => r.GetAnswerAsync(preparation.Id, "Technical", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(
            Command(preparation.InterviewInvitationId, "New answer"), CancellationToken.None);

        existing.AnswerText.Should().Be("New answer");
        existing.IsPracticed.Should().BeTrue();
        _preparationRepository.Verify(r => r.UpdateAnswer(existing), Times.Once);
        _preparationRepository.Verify(
            r => r.AddAnswerAsync(It.IsAny<PreparationAnswer>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAnswerTextChanged_RemovesExistingEvaluation()
    {
        var preparation = CreatePreparation();
        var existing = CreateAnswer(preparation.Id, answerText: "Old answer");
        var evaluation = InterviewPreparationEvaluation.Create(
            new CreateEvaluationPayload(preparation.Id, 70, "Summary", "[]", "[]", "[]", 1, 1));
        SetUpPreparation(preparation, answers: new List<PreparationAnswer> { existing }, evaluation);
        _preparationRepository
            .Setup(r => r.GetAnswerAsync(preparation.Id, "Technical", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(
            Command(preparation.InterviewInvitationId, "Changed answer"), CancellationToken.None);

        _preparationRepository.Verify(
            r => r.RemoveEvaluationByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOnlyPracticedFlagChanged_KeepsExistingEvaluation()
    {
        var preparation = CreatePreparation();
        var answer = CreateAnswer(preparation.Id, answerText: "Same answer", practiced: false);
        var evaluation = InterviewPreparationEvaluation.Create(
            new CreateEvaluationPayload(preparation.Id, 70, "Summary", "[]", "[]", "[]", 1, 1));
        SetUpPreparation(preparation, answers: new List<PreparationAnswer> { answer }, evaluation);
        _preparationRepository
            .Setup(r => r.GetAnswerAsync(preparation.Id, "Technical", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

        await _handler.Handle(
            Command(preparation.InterviewInvitationId, "Same answer"), CancellationToken.None);

        answer.IsPracticed.Should().BeTrue();
        _preparationRepository.Verify(
            r => r.RemoveEvaluationByPreparationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _preparationRepository.Verify(r => r.UpdateAnswer(answer), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsUnknown_ThrowsBadRequestException()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation);

        var command = Command(preparation.InterviewInvitationId) with { Category = "Nonsense" };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Question not found in this preparation.");
        _preparationRepository.Verify(
            r => r.AddAnswerAsync(It.IsAny<PreparationAnswer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenQuestionIndexIsOutOfRange_ThrowsBadRequestException()
    {
        var preparation = CreatePreparation();
        SetUpPreparation(preparation);

        var command = Command(preparation.InterviewInvitationId) with { QuestionIndex = 5 };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Question not found in this preparation.");
    }

    [Fact]
    public async Task Handle_WhenPreparationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewPreparationEntity?)null);

        var act = () => _handler.Handle(Command(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview preparation not found.");
    }

    [Fact]
    public async Task Handle_WhenPreparationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var preparation = CreatePreparation(talentUserId: "talent-2");
        SetUpPreparation(preparation);

        var act = () => _handler.Handle(
            Command(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview preparation.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInterviewAlreadyTookPlace_ThrowsForbiddenException()
    {
        var preparation = CreatePreparation(scheduledAt: DateTime.UtcNow.AddHours(-2));
        SetUpPreparation(preparation);

        var act = () => _handler.Handle(
            Command(preparation.InterviewInvitationId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Preparation is no longer accessible as the interview has already taken place.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

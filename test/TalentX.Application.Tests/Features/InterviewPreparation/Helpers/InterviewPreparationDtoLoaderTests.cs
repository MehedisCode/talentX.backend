using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation.Helpers;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Helpers;

public class InterviewPreparationDtoLoaderTests
{
    private const string TalentUserId = "talent-1";
    private static readonly TimeSpan AccessWindow = TimeSpan.FromMinutes(30);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();

    public InterviewPreparationDtoLoaderTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
    }

    private static InterviewPreparationEntity CreatePreparation(DateTime scheduledAt)
    {
        return InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: scheduledAt,
                TechnicalQuestions:
                    """[{"question":"What is a closure?","category":"Technical","tip":null}]""",
                BehavioralQuestions: "[]",
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: "[]",
                SuggestedRevisionAreas: "[]"));
    }

    [Fact]
    public async Task LoadAsync_LoadsAnswersAndEvaluationAndMapsThemIntoTheDto()
    {
        var preparation = CreatePreparation(DateTime.UtcNow.AddHours(2));
        var answer = PreparationAnswer.Create(new CreatePreparationAnswerPayload(
            preparation.Id, "Technical", 0, "What is a closure?",
            "A closure captures its lexical scope.", true));
        var evaluation = InterviewPreparationEvaluation.Create(
            new CreateEvaluationPayload(preparation.Id, 80, "Solid.", "[]", "[]", "[]", 1, 1));
        _preparationRepository
            .Setup(r => r.GetAnswersByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PreparationAnswer> { answer });
        _preparationRepository
            .Setup(r => r.GetEvaluationByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluation);

        var dto = await InterviewPreparationDtoLoader.LoadAsync(
            _unitOfWork.Object, preparation, AccessWindow, CancellationToken.None);

        dto.Answers.Should().ContainSingle().Which.Answer.Should()
            .Be("A closure captures its lexical scope.");
        dto.Evaluation!.OverallScore.Should().Be(80);
        dto.IsAccessible.Should().BeTrue();
        _preparationRepository.Verify(
            r => r.GetAnswersByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _preparationRepository.Verify(
            r => r.GetEvaluationByPreparationIdAsync(preparation.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WhenInterviewFellOutsideTheAccessWindow_MarksDtoInaccessible()
    {
        var preparation = CreatePreparation(DateTime.UtcNow.AddHours(-2));

        var dto = await InterviewPreparationDtoLoader.LoadAsync(
            _unitOfWork.Object, preparation, AccessWindow, CancellationToken.None);

        dto.IsAccessible.Should().BeFalse();
        dto.Answers.Should().BeEmpty();
        dto.Evaluation.Should().BeNull();
    }
}

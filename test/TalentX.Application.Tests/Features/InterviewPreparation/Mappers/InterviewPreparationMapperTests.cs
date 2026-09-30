using FluentAssertions;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Mappers;

public class InterviewPreparationMapperTests
{
    private static readonly TimeSpan AccessWindow = TimeSpan.FromMinutes(30);

    private static InterviewPreparationEntity NewPreparation(
        string technicalQuestions = """[{"question":"What is a closure?","category":"Technical","tip":"Scope"}]""",
        string behavioralQuestions = """[{"question":"Tell me about a conflict","category":"Behavioral","tip":"STAR"}]""",
        string experienceBasedQuestions = "[]",
        string roleSpecificQuestions = "[]",
        string keyTopicsToPrepare = """["Idempotency"]""",
        string suggestedRevisionAreas = """["SQL tuning"]""",
        DateTime? scheduledAt = null)
    {
        var preparation = InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: "talent-1",
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: technicalQuestions,
                BehavioralQuestions: behavioralQuestions,
                ExperienceBasedQuestions: experienceBasedQuestions,
                RoleSpecificQuestions: roleSpecificQuestions,
                KeyTopicsToPrepare: keyTopicsToPrepare,
                SuggestedRevisionAreas: suggestedRevisionAreas));

        return preparation;
    }

    [Fact]
    public void ToDto_WithValidJson_DeserializesQuestionAndTopicLists()
    {
        var preparation = NewPreparation();

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.TechnicalQuestions.Should().ContainSingle()
            .Which.Should().Be(new PrepQuestion("What is a closure?", "Technical", "Scope"));
        dto.BehavioralQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("Tell me about a conflict");
        dto.KeyTopicsToPrepare.Should().Equal("Idempotency");
        dto.SuggestedRevisionAreas.Should().Equal("SQL tuning");
        dto.ExperienceBasedQuestions.Should().BeEmpty();
        dto.RoleSpecificQuestions.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithMalformedJson_ReturnsEmptyListsWithoutThrowing()
    {
        var preparation = NewPreparation(
            technicalQuestions: "{not valid json",
            behavioralQuestions: """{"key":"object instead of list"}""",
            keyTopicsToPrepare: "42",
            suggestedRevisionAreas: "");

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.TechnicalQuestions.Should().BeEmpty();
        dto.BehavioralQuestions.Should().BeEmpty();
        dto.KeyTopicsToPrepare.Should().BeEmpty();
        dto.SuggestedRevisionAreas.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithBlankQuestionFields_ReturnsEmptyLists()
    {
        var preparation = NewPreparation(
            technicalQuestions: "",
            behavioralQuestions: "   ",
            keyTopicsToPrepare: "",
            suggestedRevisionAreas: "");

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.TechnicalQuestions.Should().BeEmpty();
        dto.BehavioralQuestions.Should().BeEmpty();
        dto.KeyTopicsToPrepare.Should().BeEmpty();
        dto.SuggestedRevisionAreas.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WhenAdditionalQuestionsAbsent_ReturnsEmptyList()
    {
        var preparation = NewPreparation();

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.AdditionalQuestions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ToDto_WhenAdditionalQuestionsPresent_DeserializesThem()
    {
        var preparation = NewPreparation();
        preparation.SetAdditionalQuestions(
            """[{"question":"What is DI?","category":"Technical","tip":"Containers"}]""");

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.AdditionalQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("What is DI?");
    }

    [Fact]
    public void ToDto_MapsScalarFieldsAndStatusString()
    {
        var preparation = NewPreparation();

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.Id.Should().Be(preparation.Id);
        dto.InterviewInvitationId.Should().Be(preparation.InterviewInvitationId);
        dto.JobId.Should().Be(preparation.JobId);
        dto.JobTitle.Should().Be("Backend Engineer");
        dto.CompanyName.Should().Be("Acme");
        dto.InterviewType.Should().Be("OnCall");
        dto.ScheduledAt.Should().Be(preparation.ScheduledAt);
        dto.Status.Should().Be("Generated");
        dto.GenerationCount.Should().Be(1);
        dto.GeneratedAt.Should().Be(preparation.CreatedAt);
        dto.Disclaimer.Should().NotBeEmpty();
    }

    [Fact]
    public void ToDto_ComputesIsAccessibleTrue_ForUpcomingInterview()
    {
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddHours(3));

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ComputesIsAccessibleFalse_WhenInterviewWindowExpired()
    {
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddHours(-2));

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.IsAccessible.Should().BeFalse();
    }

    [Fact]
    public void ToDto_WithAnswersAndEvaluation_MapsThem()
    {
        var preparation = NewPreparation();
        var answer = PreparationAnswer.Create(new CreatePreparationAnswerPayload(
            preparation.Id, "Technical", 0, "What is a closure?", "A scoped function", true));
        var evaluation = InterviewPreparationEvaluation.Create(new CreateEvaluationPayload(
            preparation.Id, 82, "Strong answers.", """["Examples"]""", """["Metrics"]""",
            """[{"category":"Technical","questionIndex":0,"score":80,"feedback":"Nice.","suggestedAnswer":""}]""",
            AnsweredCount: 1, TotalCount: 2));

        var dto = InterviewPreparationMapper.ToDto(
            preparation, AccessWindow, new[] { answer }, evaluation);

        dto.Answers.Should().ContainSingle().Which.Should().Be(
            new PrepAnswerDto("Technical", 0, "What is a closure?", "A scoped function", true));
        dto.Evaluation.Should().NotBeNull();
        dto.Evaluation!.OverallScore.Should().Be(82);
        dto.Evaluation.AnsweredCount.Should().Be(1);
        dto.Evaluation.TotalCount.Should().Be(2);
        dto.Evaluation.Strengths.Should().Equal("Examples");
        dto.Evaluation.QuestionFeedback.Should().ContainSingle()
            .Which.Score.Should().Be(80);
    }

    [Fact]
    public void ToDto_WithoutAnswersOrEvaluation_ReturnsEmptyAnswersAndNullEvaluation()
    {
        var preparation = NewPreparation();

        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        dto.Answers.Should().BeEmpty();
        dto.Evaluation.Should().BeNull();
    }

    [Fact]
    public void FindQuestion_ForKnownCategoryAndIndex_ReturnsStoredQuestion()
    {
        var preparation = NewPreparation();

        var question = InterviewPreparationMapper.FindQuestion(preparation, "Technical", 0);

        question.Should().Be(new PrepQuestion("What is a closure?", "Technical", "Scope"));
    }

    [Fact]
    public void FindQuestion_ForUnknownCategoryOrOutOfRangeIndex_ReturnsNull()
    {
        var preparation = NewPreparation();

        InterviewPreparationMapper.FindQuestion(preparation, "Nonsense", 0).Should().BeNull();
        InterviewPreparationMapper.FindQuestion(preparation, "Technical", 5).Should().BeNull();
        InterviewPreparationMapper.FindQuestion(preparation, "Technical", -1).Should().BeNull();
        InterviewPreparationMapper.FindQuestion(preparation, "Additional", 0).Should().BeNull();
    }

    [Fact]
    public void CountQuestions_CountsAllCategoriesIncludingAdditional()
    {
        var preparation = NewPreparation();
        preparation.SetAdditionalQuestions(
            """[{"question":"What is DI?","category":"Additional","tip":null}]""");

        InterviewPreparationMapper.CountQuestions(preparation).Should().Be(3);
    }
}

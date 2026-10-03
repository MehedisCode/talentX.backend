using FluentAssertions;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation;

public class AnswersEvaluationContextFactoryTests
{
    private const string TalentUserId = "talent-1";

    private static InterviewPreparationEntity CreatePreparation(
        string technicalQuestions = """[{"question":"What is a closure?","category":"Technical","tip":null}]""",
        string behavioralQuestions = "[]",
        string additionalQuestions = "[]")
    {
        var preparation = InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: technicalQuestions,
                BehavioralQuestions: behavioralQuestions,
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: "[]",
                SuggestedRevisionAreas: "[]"));

        preparation.SetAdditionalQuestions(additionalQuestions);
        return preparation;
    }

    private static PreparationAnswer Answer(
        Guid preparationId, string category, int index, string question, string text)
    {
        return PreparationAnswer.Create(new CreatePreparationAnswerPayload(
            preparationId, category, index, question, text, true));
    }

    [Fact]
    public void CreateContext_MapsPreparationFieldsAndAnsweredQuestions()
    {
        var preparation = CreatePreparation();
        var answered = new List<PreparationAnswer>
        {
            Answer(preparation.Id, "Technical", 0, "What is a closure?",
                "A closure captures its lexical scope.")
        };

        var context = AnswersEvaluationContextFactory.CreateContext(
            preparation, "Ada Lovelace", answered);

        context.JobTitle.Should().Be("Backend Engineer");
        context.CompanyName.Should().Be("Acme");
        context.InterviewType.Should().Be("OnCall");
        context.TalentName.Should().Be("Ada Lovelace");
        context.AnsweredQuestions.Should().ContainSingle();
        context.AnsweredQuestions[0].Should().BeEquivalentTo(
            new AnsweredQuestion("Technical", 0, "What is a closure?",
                "A closure captures its lexical scope."));
    }

    [Fact]
    public void CreateContext_WhenTalentNameIsMissing_FallsBackToCandidate()
    {
        var preparation = CreatePreparation();

        var context = AnswersEvaluationContextFactory.CreateContext(
            preparation, null, new List<PreparationAnswer>());

        context.TalentName.Should().Be("Candidate");
    }

    [Fact]
    public void CreateContext_CountsQuestionsAcrossEveryCategoryIncludingAdditional()
    {
        var preparation = CreatePreparation(
            technicalQuestions: """[{"question":"Q1","category":"Technical","tip":null}]""",
            behavioralQuestions: """[{"question":"Q2","category":"Behavioral","tip":null}]""",
            additionalQuestions: """[{"question":"Q3","category":"Additional","tip":null}]""");

        var context = AnswersEvaluationContextFactory.CreateContext(
            preparation, "Ada Lovelace", new List<PreparationAnswer>());

        context.TotalQuestionCount.Should().Be(3);
    }
}

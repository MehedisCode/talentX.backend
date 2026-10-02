using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Domain.Entities;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation;

public static class AnswersEvaluationContextFactory
{
    public static AnswersEvaluationContext CreateContext(
        InterviewPreparationEntity preparation,
        string? talentName,
        IReadOnlyList<PreparationAnswer> answeredQuestions)
    {
        return new AnswersEvaluationContext
        {
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            TalentName = talentName ?? "Candidate",
            InterviewType = preparation.InterviewType,
            TotalQuestionCount = InterviewPreparationMapper.CountQuestions(preparation),
            AnsweredQuestions = answeredQuestions
                .Select(a => new AnsweredQuestion(
                    a.Category, a.QuestionIndex, a.QuestionText, a.AnswerText))
                .ToList()
        };
    }
}

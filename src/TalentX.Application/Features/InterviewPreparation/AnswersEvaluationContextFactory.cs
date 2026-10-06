using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Domain.Entities;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation;

public static class AnswersEvaluationContextFactory
{
    // Rough chars-per-token estimate used only for budgeting, not accounting.
    private const int CharsPerToken = 4;

    public static AnswersEvaluationContext CreateContext(
        InterviewPreparationEntity preparation,
        string? talentName,
        IReadOnlyList<PreparationAnswer> answeredQuestions,
        int inputTokenBudget = InterviewPreparationSettings.DefaultEvaluationInputTokenBudget,
        int perAnswerTokenBudget = InterviewPreparationSettings.DefaultEvaluationPerAnswerTokenBudget)
    {
        // Each answer gets an equal slice of the total budget, never more than
        // the per-answer cap: min(perAnswer, total / count), in characters.
        var answerCharBudget = Math.Max(1, answeredQuestions.Count == 0
            ? perAnswerTokenBudget * CharsPerToken
            : Math.Min(
                perAnswerTokenBudget,
                inputTokenBudget / answeredQuestions.Count) * CharsPerToken);

        return new AnswersEvaluationContext
        {
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            TalentName = talentName ?? "Candidate",
            InterviewType = preparation.InterviewType,
            TotalQuestionCount = InterviewPreparationMapper.CountQuestions(preparation),
            AnsweredQuestions = answeredQuestions
                .Select(a => new AnsweredQuestion(
                    a.Category, a.QuestionIndex, a.QuestionText,
                    Truncate(a.AnswerText, answerCharBudget)))
                .ToList()
        };
    }

    private static string Truncate(string text, int maxChars)
    {
        if (maxChars < 1)
        {
            return string.Empty;
        }
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
        {
            return text;
        }
        return text[..maxChars] + "…";
    }
}

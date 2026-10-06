using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Tools.InterviewEvaluationHarness;

/// <summary>
/// One labeled case from the golden set: practice questions with answers a human
/// graded via <see cref="ExpectedScore"/> (0-100, banded like the frontend).
/// </summary>
public sealed record GoldenCase(
    string Id,
    int ExpectedScore,
    string JobTitle,
    string CompanyName,
    string InterviewType,
    string TalentName,
    int TotalQuestionCount,
    List<GoldenAnswer> Questions)
{
    public AnswersEvaluationContext ToContext() => new()
    {
        JobTitle = JobTitle,
        CompanyName = CompanyName,
        TalentName = TalentName,
        InterviewType = InterviewType,
        TotalQuestionCount = TotalQuestionCount,
        AnsweredQuestions = Questions
            .Select(q => new AnsweredQuestion(q.Category, q.QuestionIndex, q.Question, q.Answer))
            .ToList()
    };
}

public sealed record GoldenAnswer(string Category, int QuestionIndex, string Question, string Answer);

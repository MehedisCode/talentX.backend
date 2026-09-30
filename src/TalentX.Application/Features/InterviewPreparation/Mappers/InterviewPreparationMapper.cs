using System.Text.Json;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation.Mappers;

public static class InterviewPreparationMapper
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static InterviewPreparationDto ToDto(
        Domain.Entities.InterviewPreparation preparation,
        TimeSpan accessWindow,
        IReadOnlyList<PreparationAnswer>? answers = null,
        InterviewPreparationEvaluation? evaluation = null)
    {
        return new InterviewPreparationDto
        {
            Id = preparation.Id,
            InterviewInvitationId = preparation.InterviewInvitationId,
            JobId = preparation.JobId,
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            InterviewType = preparation.InterviewType,
            ScheduledAt = preparation.ScheduledAt,
            Status = preparation.Status.ToString(),
            TechnicalQuestions = DeserializeList<PrepQuestion>(preparation.TechnicalQuestions),
            BehavioralQuestions = DeserializeList<PrepQuestion>(preparation.BehavioralQuestions),
            ExperienceBasedQuestions = DeserializeList<PrepQuestion>(preparation.ExperienceBasedQuestions),
            RoleSpecificQuestions = DeserializeList<PrepQuestion>(preparation.RoleSpecificQuestions),
            KeyTopicsToPrepare = DeserializeList<string>(preparation.KeyTopicsToPrepare),
            SuggestedRevisionAreas = DeserializeList<string>(preparation.SuggestedRevisionAreas),
            AdditionalQuestions = DeserializeList<PrepQuestion>(preparation.AdditionalQuestions),
            GenerationCount = preparation.GenerationCount,
            GeneratedAt = preparation.CreatedAt,
            IsAccessible = preparation.IsAccessible(accessWindow),
            Answers = (answers ?? Array.Empty<PreparationAnswer>())
                .Select(a => new PrepAnswerDto(
                    a.Category, a.QuestionIndex, a.QuestionText, a.AnswerText, a.IsPracticed))
                .ToList(),
            Evaluation = evaluation is null ? null : ToEvaluationDto(evaluation)
        };
    }

    public static EvaluationDto ToEvaluationDto(InterviewPreparationEvaluation evaluation)
    {
        return new EvaluationDto
        {
            OverallScore = evaluation.OverallScore,
            Summary = evaluation.Summary,
            Strengths = DeserializeList<string>(evaluation.Strengths),
            AreasToImprove = DeserializeList<string>(evaluation.AreasToImprove),
            QuestionFeedback = DeserializeList<QuestionFeedbackDto>(evaluation.QuestionFeedback),
            AnsweredCount = evaluation.AnsweredCount,
            TotalCount = evaluation.TotalCount,
            EvaluatedAt = evaluation.EvaluatedAt
        };
    }

    /// <summary>
    /// Resolves the authoritative stored question for a category/index pair so answer
    /// rows never trust client-supplied question text. Returns null for unknown
    /// categories or out-of-range indexes.
    /// </summary>
    public static PrepQuestion? FindQuestion(
        Domain.Entities.InterviewPreparation preparation, string category, int questionIndex)
    {
        var questions = category switch
        {
            "Technical" => DeserializeList<PrepQuestion>(preparation.TechnicalQuestions),
            "Behavioral" => DeserializeList<PrepQuestion>(preparation.BehavioralQuestions),
            "Experience-Based" => DeserializeList<PrepQuestion>(preparation.ExperienceBasedQuestions),
            "Role-Specific" => DeserializeList<PrepQuestion>(preparation.RoleSpecificQuestions),
            "Additional" => DeserializeList<PrepQuestion>(preparation.AdditionalQuestions),
            _ => null
        };

        if (questions is null || questionIndex < 0 || questionIndex >= questions.Count)
            return null;

        return questions[questionIndex];
    }

    public static int CountQuestions(Domain.Entities.InterviewPreparation preparation)
    {
        return DeserializeList<PrepQuestion>(preparation.TechnicalQuestions).Count
            + DeserializeList<PrepQuestion>(preparation.BehavioralQuestions).Count
            + DeserializeList<PrepQuestion>(preparation.ExperienceBasedQuestions).Count
            + DeserializeList<PrepQuestion>(preparation.RoleSpecificQuestions).Count
            + DeserializeList<PrepQuestion>(preparation.AdditionalQuestions).Count;
    }

    // Blank or corrupt JSON degrades to [] instead of failing the request (silent by design)
    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

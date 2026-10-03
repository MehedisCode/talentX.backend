using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Common.Interfaces;

public interface IInterviewPreparationAiService
{
    Task<InterviewPreparationResult> GeneratePreparationAsync(
        InterviewPreparationContext context,
        CancellationToken cancellationToken = default);

    Task<AdditionalQuestionsResult> GenerateAdditionalQuestionsAsync(
        AdditionalQuestionsContext context,
        CancellationToken cancellationToken = default);

    Task<AnswersEvaluationResult> EvaluateAnswersAsync(
        AnswersEvaluationContext context,
        CancellationToken cancellationToken = default);
}

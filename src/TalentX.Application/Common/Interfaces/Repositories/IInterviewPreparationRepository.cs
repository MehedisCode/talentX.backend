using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IInterviewPreparationRepository
{
    Task<InterviewPreparation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InterviewPreparation?> GetByInvitationIdAsync(
        Guid interviewInvitationId, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewPreparation>> GetByTalentUserIdAsync(
        string talentUserId, CancellationToken ct = default);
    Task AddAsync(InterviewPreparation entity, CancellationToken ct = default);
    void Update(InterviewPreparation entity);

    // Daily quotas count generation events (one row per AI call), never
    // GenerationCount sums — creations and regenerations are tracked separately.
    Task<int> CountInitialGenerationsTodayByTalentAsync(
        string talentUserId, CancellationToken ct = default);
    Task<int> CountAllGenerationsTodayByTalentAsync(
        string talentUserId, CancellationToken ct = default);
    Task AddGenerationAsync(
        InterviewPreparationGeneration entity, CancellationToken ct = default);

    Task<IReadOnlyList<PreparationAnswer>> GetAnswersByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default);
    Task<IReadOnlyList<PreparationAnswer>> GetAnswersByPreparationIdsAsync(
        IReadOnlyCollection<Guid> interviewPreparationIds, CancellationToken ct = default);
    Task<PreparationAnswer?> GetAnswerAsync(
        Guid interviewPreparationId, string category, int questionIndex,
        CancellationToken ct = default);
    Task AddAnswerAsync(PreparationAnswer entity, CancellationToken ct = default);
    void UpdateAnswer(PreparationAnswer entity);
    Task RemoveAnswersByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default);

    Task<InterviewPreparationEvaluation?> GetEvaluationByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewPreparationEvaluation>> GetEvaluationsByPreparationIdsAsync(
        IReadOnlyCollection<Guid> interviewPreparationIds, CancellationToken ct = default);
    Task AddEvaluationAsync(
        InterviewPreparationEvaluation entity, CancellationToken ct = default);
    void UpdateEvaluation(InterviewPreparationEvaluation entity);
    Task RemoveEvaluationByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default);
}

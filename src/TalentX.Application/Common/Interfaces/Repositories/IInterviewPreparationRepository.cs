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
    Task<int> CountTodayByTalentAsync(string talentUserId, CancellationToken ct = default);
}

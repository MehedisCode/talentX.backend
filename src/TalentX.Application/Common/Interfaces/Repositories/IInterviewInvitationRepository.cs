using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IInterviewInvitationRepository
{
    Task<InterviewInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByIdWithTimeslotsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewInvitation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewInvitation>> GetByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default);
    void Update(InterviewInvitation invitation);
}

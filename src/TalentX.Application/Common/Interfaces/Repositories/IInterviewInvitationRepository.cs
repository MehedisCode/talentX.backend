using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IInterviewInvitationRepository
{
    Task<InterviewInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByIdWithTimeslotsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated, with Job/JobApplication/Timeslots eager-loaded in the same query (no N+1).
    /// </summary>
    Task<PaginatedList<InterviewInvitation>> GetByJobIdPaginatedAsync(Guid jobId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated, with Job/Timeslots eager-loaded in the same query (no N+1).
    /// </summary>
    Task<PaginatedList<InterviewInvitation>> GetByTalentUserIdPaginatedAsync(string talentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default);
    void Update(InterviewInvitation invitation);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
}

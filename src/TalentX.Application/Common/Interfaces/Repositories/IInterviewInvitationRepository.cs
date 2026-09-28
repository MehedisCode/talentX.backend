using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;

public sealed record InterviewInvitationListFilter(
    InterviewInvitationStatus? Status = null,
    InterviewType? Type = null);

public interface IInterviewInvitationRepository
{
    Task<InterviewInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByIdWithTimeslotsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated, with Job/JobApplication/Timeslots eager-loaded in the same query (no N+1).
    /// </summary>
    Task<PaginatedList<InterviewInvitation>> GetByJobIdPaginatedAsync(Guid jobId, InterviewInvitationListFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated, with Job/Timeslots eager-loaded in the same query (no N+1).
    /// </summary>
    Task<PaginatedList<InterviewInvitation>> GetByTalentUserIdPaginatedAsync(string talentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<InterviewInvitation?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepted invitations for a talent with Timeslots/Job/JobApplication loaded, including those
    /// whose job was closed or soft-deleted (company reviews outlive the job posting).
    /// </summary>
    Task<IReadOnlyList<InterviewInvitation>> GetAcceptedByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepted invitations (one per interview round) for a single application, with Timeslots loaded.
    /// </summary>
    Task<IReadOnlyList<InterviewInvitation>> GetAcceptedByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default);
    void Update(InterviewInvitation invitation);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);

    // Returns how many interview invitations were created in the range, as one count (cheap for all-time totals). Cancelled invitations are excluded.
    Task<int> CountCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    // Returns how many interview invitations were created in the range per UTC hour (groupByHour) or UTC day; empty buckets are absent.
    Task<IReadOnlyDictionary<DateTime, int>> GetCreatedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default);
}

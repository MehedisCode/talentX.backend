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
    Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default);
    void Update(InterviewInvitation invitation);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);

    // Admin dashboard. Invitations created in range (UTC, inclusive), excluding Cancelled. One count, so an
    // all-time total does not need a bucket per day of history.
    Task<int> CountCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    // Same filter, keyed by the UTC hour (groupByHour) or UTC day start.
    Task<IReadOnlyDictionary<DateTime, int>> GetCreatedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default);
}

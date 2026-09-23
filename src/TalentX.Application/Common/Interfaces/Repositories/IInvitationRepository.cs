using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public sealed record TalentInvitationRecord(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime InvitedDate,
    DateTime ExpiresAt,
    string? Message,
    WorkArrangement WorkArrangement,
    EmploymentType EmploymentType);

public interface IInvitationRepository
{
    Task<PaginatedList<TalentInvitationRecord>> GetActiveInvitationsForTalentAsync(
        string talentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Invitation?> GetActiveByJobIdAndTalentUserIdAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    void Update(Invitation invitation);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
}

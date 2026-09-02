using TalentX.Application.Common.Models;

namespace TalentX.Application.Common.Interfaces.Repositories;

public sealed record TalentInvitationRecord(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime InvitedDate);

public interface IInvitationRepository
{
    Task<PaginatedList<TalentInvitationRecord>> GetActiveInvitationsForTalentAsync(
        string talentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

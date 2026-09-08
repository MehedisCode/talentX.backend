using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invitation?> GetActiveByJobIdAndTalentUserIdAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    void Update(Invitation invitation);
}

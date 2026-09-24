using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface ITalentNotificationPreferenceRepository
{
    Task<TalentNotificationPreference?> GetByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
    Task AddAsync(TalentNotificationPreference entity, CancellationToken cancellationToken = default);
    Task DeleteByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
}

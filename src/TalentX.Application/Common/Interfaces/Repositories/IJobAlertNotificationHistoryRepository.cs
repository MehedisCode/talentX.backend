using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobAlertNotificationHistoryRepository
{
    Task<bool> ExistsAsync(string talentId, Guid jobId, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetNotifiedTalentIdsAsync(Guid jobId, IEnumerable<string> talentIds, CancellationToken cancellationToken = default);
    Task AddAsync(JobAlertNotificationHistory history, CancellationToken cancellationToken = default);
    Task DeleteByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
}

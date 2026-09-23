using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobModerationHistoryRepository
{
    Task AddAsync(JobModerationHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobModerationHistory>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task DeleteByJobIdsAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken = default);
}

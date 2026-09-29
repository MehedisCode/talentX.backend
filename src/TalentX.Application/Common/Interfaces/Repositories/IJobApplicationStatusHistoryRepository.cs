using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobApplicationStatusHistoryRepository
{
    Task AddAsync(JobApplicationStatusHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplicationStatusHistory>> GetByApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default);
    Task DeleteByApplicationIdsAsync(IEnumerable<Guid> jobApplicationIds, CancellationToken cancellationToken = default);
}

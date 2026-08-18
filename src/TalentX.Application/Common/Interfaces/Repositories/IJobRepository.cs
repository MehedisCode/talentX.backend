using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobRepository
{
    Task<(IReadOnlyList<Job> Items, int TotalCount)> GetPublishedJobsAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    Task<Job?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);
}

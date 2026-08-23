using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);
    Task<PaginatedList<Job>> GetPublishedJobsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<Job?> GetPublishedJobByIdAsync(Guid id, CancellationToken cancellationToken);
}

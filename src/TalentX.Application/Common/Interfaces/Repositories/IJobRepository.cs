using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobRepository : IRepository<Job>
{
    Task<PaginatedList<Job>> GetPublishedJobsAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Job?> GetPublishedJobByIdAsync(Guid id, CancellationToken cancellationToken);
}

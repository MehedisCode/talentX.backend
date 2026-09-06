using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface ISavedJobRepository
{
    Task<SavedJob?> FindAsync(string talentId, Guid jobId, CancellationToken cancellationToken = default);
    Task AddAsync(SavedJob entity, CancellationToken cancellationToken = default);
    void Remove(SavedJob entity);
    Task<PaginatedList<SavedJob>> GetSavedJobsAsync(string talentId, int page, int pageSize, CancellationToken cancellationToken = default);
}

using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobAlertRepository
{
    Task<JobAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<JobAlert>> GetByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
    Task<int> CountByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
    Task AddAsync(JobAlert entity, CancellationToken cancellationToken = default);
    void Remove(JobAlert entity);
    Task<List<JobAlert>> GetActiveAlertsByTalentIdAsync(string talentId, CancellationToken cancellationToken = default);
    Task<List<JobAlert>> GetAllActiveAlertsAsync(CancellationToken cancellationToken = default);
}

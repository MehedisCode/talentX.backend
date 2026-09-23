using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobTalentMatchRepository
{
    Task<IReadOnlyList<JobTalentMatch>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<JobTalentMatch?> GetByJobIdAndTalentUserIdAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task AddAsync(JobTalentMatch match, CancellationToken cancellationToken = default);
    void Update(JobTalentMatch match);
    Task DeleteByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
}

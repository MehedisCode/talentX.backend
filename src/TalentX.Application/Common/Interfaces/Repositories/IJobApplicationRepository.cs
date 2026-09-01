using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobApplicationRepository
{
    Task<bool> ExistsForJobAndTalentAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingForJobAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetPendingByJobAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplication>> GetPendingByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplication>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
}

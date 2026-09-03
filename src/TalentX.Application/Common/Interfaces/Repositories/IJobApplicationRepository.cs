using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobApplicationRepository
{
    /// <summary>
    /// Checks whether an application already exists for the specified job and talent user.
    /// </summary>
    Task<bool> ExistsByJobIdAndTalentUserIdAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an application pending verification exists for the specified job and applicant email.
    /// </summary>
    Task<bool> ExistsPendingByJobIdAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an application pending verification for the specified job and applicant email.
    /// </summary>
    Task<JobApplication?> GetPendingByJobIdAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all applications pending verification for the specified applicant email.
    /// </summary>
    Task<IReadOnlyList<JobApplication>> GetPendingByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all applications associated with the specified job.
    /// </summary>
    Task<IReadOnlyList<JobApplication>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an application by its domain identifier.
    /// </summary>
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new job application.
    /// </summary>
    Task AddAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
}

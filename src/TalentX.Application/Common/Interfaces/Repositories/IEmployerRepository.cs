using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;

/// <summary>
/// Repository contract for database operations related to the <see cref="Employer"/> entity.
/// </summary>
public interface IEmployerRepository
{
    /// <summary>
    /// Adds a new employer entity to the repository.
    /// </summary>
    /// <param name="employer">The employer domain entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Employer employer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing employer entity in the repository.
    /// </summary>
    /// <param name="employer">The employer domain entity.</param>
    void Update(Employer employer);

    /// <summary>
    /// Gets an employer by its domain unique identifier.
    /// </summary>
    /// <param name="id">The employer GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an employer by its associated user identifier.
    /// </summary>
    /// <param name="userId">The associated user string identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an employer by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an employer with the given email exists.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of employers with optional search and status filter.
    /// </summary>
    Task<(IReadOnlyList<Employer> Items, int TotalCount)> GetPagedAsync(
        string? search,
        EmployerVerificationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Returns how many employers registered in the range, grouped by verification status.
    Task<IReadOnlyDictionary<EmployerVerificationStatus, int>> GetVerificationStatusCountsCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    // Returns when the oldest still-pending employer registered, ignoring any range. Null when nothing is pending.
    Task<DateTime?> GetOldestPendingCreatedAtAsync(CancellationToken cancellationToken = default);

    // Returns the newest employers registered in the range, newest first, up to `take`.
    Task<IReadOnlyList<Employer>> GetRecentCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        int take,
        CancellationToken cancellationToken = default);
}

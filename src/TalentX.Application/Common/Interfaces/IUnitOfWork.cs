using TalentX.Application.Common.Interfaces.Repositories;

namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Unit of work interface coordinating repository operations and transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Repository for user operations.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Repository for employer operations.
    /// </summary>
    IEmployerRepository Employers { get; }

    /// <summary>
    /// Repository for job operations.
    /// </summary>
    IJobRepository Jobs { get; }

    /// <summary>
    /// Asynchronously persists all changes made in the context to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
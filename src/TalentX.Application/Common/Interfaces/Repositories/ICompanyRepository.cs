using TalentX.Domain;

namespace TalentX.Application.Common.Interfaces.Repositories;

/// <summary>
/// Repository contract for database operations related to the <see cref="Company"/> entity.
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Adds a new company entity to the repository.
    /// </summary>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing company entity in the repository.
    /// </summary>
    void Update(Company company);

    /// <summary>
    /// Gets a company by its unique identifier.
    /// </summary>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a company by its associated employer identifier.
    /// </summary>
    Task<Company?> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets companies for a batch of employer identifiers in a single query.
    /// Used to enrich job lists without N+1 round-trips.
    /// </summary>
    Task<IReadOnlyList<Company>> GetByEmployerIdsAsync(IEnumerable<string> employerIds, CancellationToken cancellationToken = default);
}

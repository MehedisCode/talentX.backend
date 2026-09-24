using TalentX.Application.Common.Models;
using TalentX.Application.Features.Companies.DTOs;
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
    /// Deletes the company associated with the specified employer identifier.
    /// </summary>
    Task DeleteByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets companies for a batch of employer identifiers in a single query.
    /// Used to enrich job lists without N+1 round-trips.
    /// </summary>
    Task<IReadOnlyList<Company>> GetByEmployerIdsAsync(IEnumerable<string> employerIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches companies by name with industry/location/size filters and active-job aggregates.
    /// Only active published jobs (published, not deleted, not expired) contribute to
    /// <see cref="CompanySearchItem.OpeningsCount"/> (job count) and
    /// <see cref="CompanySearchItem.VacanciesCount"/> (sum of openings).
    /// SortBy: name (default, A-Z), openings, vacancies, newest. SortDir: asc (default), desc.
    /// </summary>
    Task<PaginatedList<CompanySearchItem>> GetAllAsync(
        string? keyword,
        string? industry,
        string? location,
        string? companySize,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

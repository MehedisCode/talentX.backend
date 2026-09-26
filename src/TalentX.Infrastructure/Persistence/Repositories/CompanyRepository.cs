using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Companies.DTOs;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _context.Companies.AddAsync(company, cancellationToken);
    }

    public void Update(Company company)
    {
        _context.Companies.Update(company);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.EmployerId == employerId, cancellationToken);
    }

    public async Task<Company?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim().ToLower();
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == normalizedName, cancellationToken);
    }

    public async Task DeleteByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.EmployerId == employerId, cancellationToken);

        if (company is not null)
        {
            _context.Companies.Remove(company);
        }
    }

    public async Task<IReadOnlyList<Company>> GetByEmployerIdsAsync(IEnumerable<string> employerIds, CancellationToken cancellationToken = default)
    {
        var ids = employerIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<Company>();
        }

        return await _context.Companies
            .AsNoTracking()
            .Where(c => ids.Contains(c.EmployerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanySearchResult> GetAllAsync(
        string? keyword,
        string? industry,
        string? location,
        string? companySize,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default,
        bool? hiringOnly = null)
    {
        var today = DateTime.UtcNow.Date;
        var sortDescending = sortDir == "desc";

        var filteredCompaniesQuery = _context.Companies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();
            filteredCompaniesQuery = filteredCompaniesQuery.Where(company =>
                company.Name.ToLower().Contains(normalizedKeyword) ||
                (company.Location != null && company.Location.ToLower().Contains(normalizedKeyword)) ||
                (company.Industry != null && company.Industry.ToLower().Contains(normalizedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(industry))
        {
            var normalizedIndustry = industry.Trim().ToLower();
            filteredCompaniesQuery = filteredCompaniesQuery.Where(company => company.Industry != null && company.Industry.ToLower().Contains(normalizedIndustry));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var normalizedLocation = location.Trim().ToLower();
            filteredCompaniesQuery = filteredCompaniesQuery.Where(company => company.Location != null && company.Location.ToLower().Contains(normalizedLocation));
        }

        if (!string.IsNullOrWhiteSpace(companySize))
        {
            var normalizedCompanySize = companySize.Trim().ToLower();
            filteredCompaniesQuery = filteredCompaniesQuery.Where(company => company.CompanySize != null && company.CompanySize.ToLower().Contains(normalizedCompanySize));
        }

        var activeJobsQuery = _context.Jobs
            .AsNoTracking()
            .Where(job => job.Status == JobStatus.Published && !job.IsDeleted && job.ExpireAt.Date >= today);

        if (hiringOnly == true)
        {
            filteredCompaniesQuery = filteredCompaniesQuery.Where(company =>
                activeJobsQuery.Any(job => job.EmployerId == company.EmployerId));
        }

        // Active-job aggregates per employer, fetched in one GROUP BY query.
        // Companies without active jobs simply get 0 openings / 0 vacancies.
        async Task<Dictionary<string, (int Openings, int Vacancies)>> GetJobCountsByEmployerIdAsync(
            IEnumerable<string> employerIds, CancellationToken ct)
        {
            var distinctEmployerIds = employerIds.Where(employerId => !string.IsNullOrWhiteSpace(employerId)).Distinct().ToList();
            if (distinctEmployerIds.Count == 0)
            {
                return new Dictionary<string, (int, int)>();
            }

            var jobAggregatesByEmployer = await activeJobsQuery
                .Where(job => distinctEmployerIds.Contains(job.EmployerId))
                .GroupBy(job => job.EmployerId)
                .Select(employerJobs => new
                {
                    EmployerId = employerJobs.Key,
                    Openings = employerJobs.Count(),
                    Vacancies = employerJobs.Sum(job => (int?)job.Vacancies) ?? 0
                })
                .ToListAsync(ct);

            return jobAggregatesByEmployer.ToDictionary(aggregate => aggregate.EmployerId, aggregate => (aggregate.Openings, aggregate.Vacancies));
        }

        static CompanySearchItem ToCompanySearchItem(Company company, Dictionary<string, (int Openings, int Vacancies)> jobCountsByEmployerId)
        {
            jobCountsByEmployerId.TryGetValue(company.EmployerId, out var jobCounts);
            return new CompanySearchItem(
                company.Id,
                company.EmployerId,
                company.Name,
                company.LogoStorageKey,
                company.Description,
                company.Industry,
                company.CompanySize,
                company.Website,
                company.Location,
                company.LinkedInUrl,
                company.TwitterUrl,
                company.FacebookUrl,
                company.CreatedAt,
                jobCounts.Openings,
                jobCounts.Vacancies);
        }

        // Name/newest sorts translate directly to SQL: order, page, and count
        // entirely in the database.
        if (sortBy is "name" or "newest")
        {
            IQueryable<Company> sortedCompaniesQuery = (sortBy, sortDescending) switch
            {
                ("newest", false) => filteredCompaniesQuery.OrderBy(company => company.CreatedAt).ThenBy(company => company.Id),
                ("newest", true) => filteredCompaniesQuery.OrderByDescending(company => company.CreatedAt).ThenBy(company => company.Id),
                (_, true) => filteredCompaniesQuery.OrderByDescending(company => company.Name).ThenBy(company => company.Id),
                _ => filteredCompaniesQuery.OrderBy(company => company.Name).ThenBy(company => company.Id),
            };

            var totalMatchingCompanyCount = await sortedCompaniesQuery.CountAsync(cancellationToken);

            var pagedCompanies = await sortedCompaniesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var jobCountsByEmployerId = await GetJobCountsByEmployerIdAsync(pagedCompanies.Select(company => company.EmployerId), cancellationToken);

            var pagedCompanySearchItems = pagedCompanies.Select(company => ToCompanySearchItem(company, jobCountsByEmployerId)).ToList();

            // Filtered-set totals (stable across sorting and paging): aggregate
            // active jobs of every filtered company in scalar SQL queries.
            var filteredEmployerIds = filteredCompaniesQuery.Select(company => company.EmployerId);

            var totalOpenings = await activeJobsQuery
                .Where(job => filteredEmployerIds.Contains(job.EmployerId))
                .CountAsync(cancellationToken);

            var totalVacancies = await activeJobsQuery
                .Where(job => filteredEmployerIds.Contains(job.EmployerId))
                .SumAsync(job => (int?)job.Vacancies, cancellationToken) ?? 0;

            var paged = new PaginatedList<CompanySearchItem>(pagedCompanySearchItems, page, pageSize, totalMatchingCompanyCount);

            return new CompanySearchResult(
                paged.Items,
                paged.Page,
                paged.PageSize,
                paged.TotalCount,
                paged.TotalPages,
                totalOpenings,
                totalVacancies);
        }

        // Openings/vacancies sorts depend on an aggregate EF Core can't translate
        // into a single SQL ORDER BY. The company directory is assumed small
        // enough to fit in memory: load every filtered company, compute counts
        // in one GROUP BY query, then sort and page client-side.
        var allMatchingCompanies = await filteredCompaniesQuery.ToListAsync(cancellationToken);

        var jobCountsByEmployerIdForAllMatches = await GetJobCountsByEmployerIdAsync(allMatchingCompanies.Select(company => company.EmployerId), cancellationToken);

        IEnumerable<CompanySearchItem> unsortedCompanySearchItems = allMatchingCompanies.Select(company => ToCompanySearchItem(company, jobCountsByEmployerIdForAllMatches));

        IEnumerable<CompanySearchItem> sortedCompanySearchItems = (sortBy, sortDescending) switch
        {
            ("openings", false) => unsortedCompanySearchItems.OrderBy(item => item.OpeningsCount).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("openings", true) => unsortedCompanySearchItems.OrderByDescending(item => item.OpeningsCount).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("vacancies", false) => unsortedCompanySearchItems.OrderBy(item => item.VacanciesCount).ThenBy(item => item.Name).ThenBy(item => item.Id),
            _ => unsortedCompanySearchItems.OrderByDescending(item => item.VacanciesCount).ThenBy(item => item.Name).ThenBy(item => item.Id),
        };

        var sortedCompanySearchItemsList = sortedCompanySearchItems.ToList();

        var pagedSortedCompanySearchItems = sortedCompanySearchItemsList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // All matches (and their job counts) are already in memory on this path.
        var allMatchesTotalOpenings = jobCountsByEmployerIdForAllMatches.Values.Sum(counts => counts.Openings);
        var allMatchesTotalVacancies = jobCountsByEmployerIdForAllMatches.Values.Sum(counts => counts.Vacancies);

        var sortedPaged = new PaginatedList<CompanySearchItem>(pagedSortedCompanySearchItems, page, pageSize, sortedCompanySearchItemsList.Count);

        return new CompanySearchResult(
            sortedPaged.Items,
            sortedPaged.Page,
            sortedPaged.PageSize,
            sortedPaged.TotalCount,
            sortedPaged.TotalPages,
            allMatchesTotalOpenings,
            allMatchesTotalVacancies);
    }
}

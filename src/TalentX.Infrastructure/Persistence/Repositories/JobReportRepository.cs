using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobReportRepository : IJobReportRepository
{
    private readonly ApplicationDbContext _context;

    public JobReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _context.JobReports
            .Include(r => r.Job)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<bool> ExistsForJobAndReporterAsync(Guid jobId, string reporterUserId, CancellationToken cancellationToken = default)
    {
        return await _context.JobReports
            .AnyAsync(r => r.JobId == jobId && r.ReporterUserId == reporterUserId, cancellationToken);
    }

    public async Task AddAsync(JobReport jobReport, CancellationToken cancellationToken = default)
    {
        await _context.JobReports.AddAsync(jobReport, cancellationToken);
    }

    public async Task<PaginatedList<JobReport>> GetReportsAsync(
        JobReportStatus? status,
        string? searchKeyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobReports
            .Include(r => r.Job)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.ToLower();
            query = query.Where(r => r.Job.Title.ToLower().Contains(keyword));
        }

        query = query.OrderByDescending(r => r.Status == JobReportStatus.Pending)
                     .ThenByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<JobReport>(items, page, pageSize, totalCount);
    }

    public async Task DeleteByJobIdsAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken = default)
    {
        var ids = jobIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var reports = await _context.JobReports
            .Where(r => ids.Contains(r.JobId))
            .ToListAsync(cancellationToken);

        if (reports.Count > 0)
        {
            _context.JobReports.RemoveRange(reports);
        }
    }

    public async Task DeleteByReporterUserIdAsync(string reporterUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reporterUserId))
        {
            return;
        }

        var reports = await _context.JobReports
            .Where(r => r.ReporterUserId == reporterUserId)
            .ToListAsync(cancellationToken);

        if (reports.Count > 0)
        {
            _context.JobReports.RemoveRange(reports);
        }
    }
}

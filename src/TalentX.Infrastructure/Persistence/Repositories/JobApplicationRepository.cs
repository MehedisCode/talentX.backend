using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobApplicationRepository : IJobApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public JobApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByJobIdAndTalentUserIdAsync(
        Guid jobId,
        string talentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AnyAsync(
                application => application.JobId == jobId && application.TalentUserId == talentUserId,
                cancellationToken);
    }

    public async Task<bool> ExistsPendingByJobIdAndEmailAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AnyAsync(
                application => application.JobId == jobId
                    && application.ApplicantEmail == email
                    && application.Status == Domain.Enums.ApplicationStatus.PendingVerification,
                cancellationToken);
    }

    public async Task<JobApplication?> GetPendingByJobIdAndEmailAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .FirstOrDefaultAsync(
                application => application.JobId == jobId
                    && application.ApplicantEmail == email
                    && application.Status == Domain.Enums.ApplicationStatus.PendingVerification,
                cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.ApplicantEmail == email
                && application.Status == Domain.Enums.ApplicationStatus.PendingVerification)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.JobId == jobId)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .FirstOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        JobApplication jobApplication,
        CancellationToken cancellationToken = default)
    {
        await _context.JobApplications.AddAsync(jobApplication, cancellationToken);
    }

    public async Task<PaginatedList<TalentApplicationRecord>> GetApplicationsForTalentAsync(
        string talentUserId,
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = from application in _context.JobApplications
                    join job in _context.Jobs on application.JobId equals job.Id
                    where application.TalentUserId == talentUserId
                    select new
                    {
                        application.Id,
                        application.JobId,
                        JobTitle = job.Title,
                        Company = job.CompanyName,
                        application.AppliedAt,
                        application.Status
                    };

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        query = query.OrderByDescending(a => a.AppliedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var pagedItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var records = pagedItems.Select(item => new TalentApplicationRecord(
            item.Id,
            item.JobId,
            item.JobTitle,
            item.Company,
            item.AppliedAt,
            item.Status
        )).ToList();

        return new PaginatedList<TalentApplicationRecord>(records, page, pageSize, totalCount);
    }
    public async Task<int> CountHiredByEmployerIdAsync(
        string employerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.Status == ApplicationStatus.Hired &&
                                  _context.Jobs.IgnoreQueryFilters().Any(job => job.Id == application.JobId && job.EmployerId == employerId))
            .CountAsync(cancellationToken);
    }
}

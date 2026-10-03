using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class InterviewInvitationRepository : IInterviewInvitationRepository
{
    private readonly ApplicationDbContext _context;

    public InterviewInvitationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InterviewInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<InterviewInvitation?> GetByIdWithTimeslotsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .Include(x => x.Timeslots)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PaginatedList<InterviewInvitation>> GetByJobIdPaginatedAsync(
        Guid jobId,
        InterviewInvitationListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InterviewInvitations
            .Include(x => x.JobApplication)
            .Include(x => x.Timeslots)
            .Where(x => x.JobId == jobId);

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(x => x.Type == filter.Type.Value);
        }

        query = query.OrderByDescending(x => x.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<InterviewInvitation>(items, page, pageSize, totalCount);
    }

    public async Task<PaginatedList<InterviewInvitation>> GetByTalentUserIdPaginatedAsync(
        string talentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InterviewInvitations
            .IgnoreQueryFilters()
            .Include(x => x.Job)
            .Include(x => x.Timeslots)
            .Where(x => x.TalentUserId == talentUserId)
            .OrderByDescending(x => x.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<InterviewInvitation>(items, page, pageSize, totalCount);
    }

    public async Task<InterviewInvitation?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .Include(x => x.Timeslots)
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync(x => x.JobApplicationId == applicationId, cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewInvitation>> GetAcceptedByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: reviews belong to the company, so a soft-deleted job
        // must not make the invitation disappear from the talent's list.
        return await _context.InterviewInvitations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.JobApplication)
            .Include(x => x.Timeslots)
            .Where(x => x.TalentUserId == talentUserId && x.Status == InterviewInvitationStatus.Accepted)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewInvitation>> GetAcceptedByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .AsNoTracking()
            .Include(x => x.Timeslots)
            .Where(x => x.JobApplicationId == applicationId && x.Status == InterviewInvitationStatus.Accepted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.InterviewInvitations.AddAsync(invitation, cancellationToken);
    }

    public void Update(InterviewInvitation invitation)
    {
        _context.InterviewInvitations.Update(invitation);
    }

    public async Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        var invitations = await _context.InterviewInvitations
            .IgnoreQueryFilters()
            .Include(x => x.Timeslots)
            .Where(x => x.TalentUserId == talentUserId)
            .ToListAsync(cancellationToken);

        if (invitations.Count > 0)
        {
            _context.InterviewInvitations.RemoveRange(invitations);
        }
    }

    public async Task<int> CountCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .AsNoTracking()
            .CountAsync(
                x => x.CreatedAt >= startUtc
                     && x.CreatedAt <= endUtc
                     && x.Status != InterviewInvitationStatus.Cancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetCreatedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default)
    {
        var createdAt = _context.InterviewInvitations
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startUtc
                        && x.CreatedAt <= endUtc
                        && x.Status != InterviewInvitationStatus.Cancelled)
            .Select(x => x.CreatedAt);

        return await TimeBucketCounter.CountAsync(createdAt, groupByHour, cancellationToken);
    }
}

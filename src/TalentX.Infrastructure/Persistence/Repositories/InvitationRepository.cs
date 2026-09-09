using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly ApplicationDbContext _context;

    public InvitationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<TalentInvitationRecord>> GetActiveInvitationsForTalentAsync(
        string talentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var query = from invitation in _context.Invitations
                    join job in _context.Jobs on invitation.JobId equals job.Id
                    where invitation.TalentUserId == talentUserId
                          && invitation.IsActive
                          && job.Status == JobStatus.Published
                          && job.ExpireAt >= now
                    orderby invitation.InvitedDate descending
                    select new
                    {
                        invitation.Id,
                        invitation.JobId,
                        JobTitle = job.Title,
                        Company = job.CompanyName,
                        invitation.InvitedDate
                    };

        var totalCount = await query.CountAsync(cancellationToken);
        var pagedItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var records = pagedItems.Select(item => new TalentInvitationRecord(
            item.Id,
            item.JobId,
            item.JobTitle,
            item.Company,
            item.InvitedDate
        )).ToList();

        return new PaginatedList<TalentInvitationRecord>(records, page, pageSize, totalCount);
    }
    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Invitation?> GetActiveByJobIdAndTalentUserIdAsync(
        Guid jobId,
        string talentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(
                x => x.JobId == jobId && x.TalentUserId == talentUserId && x.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invitation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.InvitedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsActiveAsync(
        Guid jobId,
        string talentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AnyAsync(
                x => x.JobId == jobId && x.TalentUserId == talentUserId && x.IsActive,
                cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.Invitations.AddAsync(invitation, cancellationToken);
    }

    public void Update(Invitation invitation)
    {
        _context.Invitations.Update(invitation);
    }
}

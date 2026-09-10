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

        var query = _context.Invitations
                    .Join(
                        _context.Jobs,
                        invitation => invitation.JobId,
                        job => job.Id,
                        (invitation, job) => new
                        {
                            invitation,
                            job
                        })
                    .Where(x =>
                        x.invitation.TalentUserId == talentUserId &&
                        x.invitation.IsActive)
                    .OrderByDescending(x => x.invitation.InvitedDate)
                    .Select(x => new
                    {
                        x.invitation.Id,
                        x.invitation.JobId,
                        JobTitle = x.job.Title,
                        Company = x.job.CompanyName,
                        x.invitation.InvitedDate
                    });


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

    public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
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

    public async Task<IReadOnlyList<Invitation>> GetByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AsNoTracking()
            .Where(x => x.TalentUserId == talentUserId)
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

using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
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

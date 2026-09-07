using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
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

    public async Task<IReadOnlyList<InterviewInvitation>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .Include(x => x.Timeslots)
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewInvitation>> GetByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .Include(x => x.Timeslots)
            .Where(x => x.TalentUserId == talentUserId)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InterviewInvitation?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.InterviewInvitations
            .Include(x => x.Timeslots)
            .FirstOrDefaultAsync(x => x.JobApplicationId == applicationId, cancellationToken);
    }

    public async Task AddAsync(InterviewInvitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.InterviewInvitations.AddAsync(invitation, cancellationToken);
    }

    public void Update(InterviewInvitation invitation)
    {
        _context.InterviewInvitations.Update(invitation);
    }
}

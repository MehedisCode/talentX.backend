using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class InterviewPreparationRepository : IInterviewPreparationRepository
{
    private readonly ApplicationDbContext _context;

    public InterviewPreparationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InterviewPreparation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.InterviewPreparations
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<InterviewPreparation?> GetByInvitationIdAsync(
        Guid interviewInvitationId, CancellationToken ct = default)
    {
        return await _context.InterviewPreparations
            .FirstOrDefaultAsync(p => p.InterviewInvitationId == interviewInvitationId, ct);
    }

    public async Task<IReadOnlyList<InterviewPreparation>> GetByTalentUserIdAsync(
        string talentUserId, CancellationToken ct = default)
    {
        return await _context.InterviewPreparations
            .Where(p => p.TalentUserId == talentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(InterviewPreparation entity, CancellationToken ct = default)
    {
        await _context.InterviewPreparations.AddAsync(entity, ct);
    }

    public void Update(InterviewPreparation entity)
    {
        _context.InterviewPreparations.Update(entity);
    }

    public async Task<int> CountTodayByTalentAsync(string talentUserId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.InterviewPreparations
            .Where(p => p.TalentUserId == talentUserId && p.CreatedAt >= today)
            .SumAsync(p => p.GenerationCount, ct);
    }
}

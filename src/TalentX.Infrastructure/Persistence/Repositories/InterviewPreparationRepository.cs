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

    public async Task<IReadOnlyList<PreparationAnswer>> GetAnswersByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default)
    {
        return await _context.PreparationAnswers
            .Where(a => a.InterviewPreparationId == interviewPreparationId)
            .OrderBy(a => a.Category).ThenBy(a => a.QuestionIndex)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PreparationAnswer>> GetAnswersByPreparationIdsAsync(
        IReadOnlyCollection<Guid> interviewPreparationIds, CancellationToken ct = default)
    {
        return await _context.PreparationAnswers
            .Where(a => interviewPreparationIds.Contains(a.InterviewPreparationId))
            .OrderBy(a => a.Category).ThenBy(a => a.QuestionIndex)
            .ToListAsync(ct);
    }

    public async Task<PreparationAnswer?> GetAnswerAsync(
        Guid interviewPreparationId, string category, int questionIndex,
        CancellationToken ct = default)
    {
        return await _context.PreparationAnswers
            .FirstOrDefaultAsync(a =>
                a.InterviewPreparationId == interviewPreparationId
                && a.Category == category
                && a.QuestionIndex == questionIndex, ct);
    }

    public async Task AddAnswerAsync(PreparationAnswer entity, CancellationToken ct = default)
    {
        await _context.PreparationAnswers.AddAsync(entity, ct);
    }

    public void UpdateAnswer(PreparationAnswer entity)
    {
        _context.PreparationAnswers.Update(entity);
    }

    public async Task RemoveAnswersByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default)
    {
        var answers = await _context.PreparationAnswers
            .Where(a => a.InterviewPreparationId == interviewPreparationId)
            .ToListAsync(ct);
        _context.PreparationAnswers.RemoveRange(answers);
    }

    public async Task<InterviewPreparationEvaluation?> GetEvaluationByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default)
    {
        return await _context.InterviewPreparationEvaluations
            .FirstOrDefaultAsync(e => e.InterviewPreparationId == interviewPreparationId, ct);
    }

    public async Task<IReadOnlyList<InterviewPreparationEvaluation>> GetEvaluationsByPreparationIdsAsync(
        IReadOnlyCollection<Guid> interviewPreparationIds, CancellationToken ct = default)
    {
        return await _context.InterviewPreparationEvaluations
            .Where(e => interviewPreparationIds.Contains(e.InterviewPreparationId))
            .ToListAsync(ct);
    }

    public async Task AddEvaluationAsync(
        InterviewPreparationEvaluation entity, CancellationToken ct = default)
    {
        await _context.InterviewPreparationEvaluations.AddAsync(entity, ct);
    }

    public void UpdateEvaluation(InterviewPreparationEvaluation entity)
    {
        _context.InterviewPreparationEvaluations.Update(entity);
    }

    public async Task RemoveEvaluationByPreparationIdAsync(
        Guid interviewPreparationId, CancellationToken ct = default)
    {
        var evaluation = await _context.InterviewPreparationEvaluations
            .FirstOrDefaultAsync(e => e.InterviewPreparationId == interviewPreparationId, ct);
        if (evaluation is not null)
        {
            _context.InterviewPreparationEvaluations.Remove(evaluation);
        }
    }
}

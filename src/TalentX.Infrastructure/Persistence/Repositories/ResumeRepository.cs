using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class ResumeRepository : IResumeRepository
{
    private readonly ApplicationDbContext _context;

    public ResumeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Resume>> GetAllByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .Where(resume => resume.UserId == userId)
            .OrderByDescending(resume => resume.IsDefault)
            .ThenByDescending(resume => resume.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Resume?> GetDefaultByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .Where(resume => resume.UserId == userId)
            .OrderByDescending(resume => resume.IsDefault)
            .ThenByDescending(resume => resume.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Resume?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await GetDefaultByUserIdAsync(userId, cancellationToken);
    }

    public async Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .FirstOrDefaultAsync(resume => resume.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByStorageKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .AnyAsync(resume => resume.StorageKey == storageKey, cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Resumes
            .CountAsync(resume => resume.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        await _context.Resumes.AddAsync(resume, cancellationToken);
    }

    public void Update(Resume resume)
    {
        _context.Resumes.Update(resume);
    }

    public void Delete(Resume resume)
    {
        _context.Resumes.Remove(resume);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var resumes = await _context.Resumes
            .Where(resume => resume.UserId == userId)
            .ToListAsync(cancellationToken);

        if (resumes.Count > 0)
        {
            _context.Resumes.RemoveRange(resumes);
        }
    }
}

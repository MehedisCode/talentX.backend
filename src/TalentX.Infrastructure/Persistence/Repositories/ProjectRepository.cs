using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Project>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(project => project.UserId == userId)
            .OrderByDescending(project => project.StartDate)
            .ThenBy(project => project.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public void Update(Project project)
    {
        _context.Projects.Update(project);
    }

    public void Delete(Project project)
    {
        _context.Projects.Remove(project);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .Where(project => project.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.Projects.RemoveRange(projects);
    }
}

using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IEmployerRepository"/>.
/// </summary>
public sealed class EmployerRepository : IEmployerRepository
{
    private readonly ApplicationDbContext _context;

    public EmployerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Employer employer, CancellationToken cancellationToken = default)
    {
        await _context.Employers.AddAsync(employer, cancellationToken);
    }

    public void Update(Employer employer)
    {
        _context.Employers.Update(employer);
    }

    public async Task<Employer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
    }

    public async Task<Employer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .AnyAsync(e => e.Email == email, cancellationToken);
    }
}

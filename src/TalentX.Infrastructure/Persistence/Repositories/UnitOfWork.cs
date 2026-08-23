using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public IJobRepository Jobs { get; }
    public IUserRepository Users { get; }
    public IEmployerRepository Employers { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Jobs = new JobRepository(_context);
        Users = new UserRepository(_context);
        Employers = new EmployerRepository(_context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
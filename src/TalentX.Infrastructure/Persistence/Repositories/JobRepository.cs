using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await _context.Set<Job>().AddAsync(job, cancellationToken);
    }

    public void Update(Job job)
    {
        _context.Set<Job>().Update(job);
    }
}

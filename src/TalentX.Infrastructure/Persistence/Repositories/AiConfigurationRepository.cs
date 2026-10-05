using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class AiConfigurationRepository : IAiConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public AiConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AiConfiguration?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiConfigurations.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(AiConfiguration config, CancellationToken cancellationToken = default)
    {
        await _context.AiConfigurations.AddAsync(config, cancellationToken);
    }

    public void Update(AiConfiguration config)
    {
        _context.AiConfigurations.Update(config);
    }
}

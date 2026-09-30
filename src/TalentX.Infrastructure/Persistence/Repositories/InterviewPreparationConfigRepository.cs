using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public class InterviewPreparationConfigRepository : IInterviewPreparationConfigRepository
{
    private readonly ApplicationDbContext _context;

    public InterviewPreparationConfigRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<InterviewPreparationConfig?> GetAsync(CancellationToken ct = default)
        => await _context.InterviewPreparationConfigs.FirstOrDefaultAsync(ct);

    public async Task AddAsync(InterviewPreparationConfig config, CancellationToken ct = default)
        => await _context.InterviewPreparationConfigs.AddAsync(config, ct);

    public void Update(InterviewPreparationConfig config)
        => _context.InterviewPreparationConfigs.Update(config);
}

using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class TalentNotificationPreferenceRepository : ITalentNotificationPreferenceRepository
{
    private readonly ApplicationDbContext _context;

    public TalentNotificationPreferenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TalentNotificationPreference?> GetByTalentIdAsync(string talentId, CancellationToken cancellationToken = default)
    {
        return await _context.TalentNotificationPreferences
            .FirstOrDefaultAsync(preference => preference.TalentId == talentId, cancellationToken);
    }

    public async Task AddAsync(TalentNotificationPreference entity, CancellationToken cancellationToken = default)
    {
        await _context.TalentNotificationPreferences.AddAsync(entity, cancellationToken);
    }
}

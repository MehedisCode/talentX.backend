using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailVerificationToken>> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .Where(t => t.ApplicantEmail == email && t.ConsumedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }
}

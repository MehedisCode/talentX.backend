using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailVerificationToken>> GetPendingByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
}

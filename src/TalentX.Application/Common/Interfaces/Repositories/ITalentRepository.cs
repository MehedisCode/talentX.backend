using TalentX.Domain;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface ITalentRepository
{
    Task<Talent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Talent?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Talent?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Talent>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountCreatedBetweenAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Talent>> GetRecentCreatedBetweenAsync(DateTime startUtc, DateTime endUtc, int take, CancellationToken cancellationToken = default);
    Task AddAsync(Talent talent, CancellationToken cancellationToken = default);
    void Update(Talent talent);
}

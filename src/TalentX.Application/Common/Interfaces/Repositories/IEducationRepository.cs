using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IEducationRepository
{
    Task<IReadOnlyList<Education>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Education?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Education education, CancellationToken cancellationToken = default);
    void Update(Education education);
    void Delete(Education education);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IResumeRepository
{
    Task<IReadOnlyList<Resume>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Resume?> GetDefaultByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Resume?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Resume resume, CancellationToken cancellationToken = default);
    void Update(Resume resume);
    void Delete(Resume resume);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

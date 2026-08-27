using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IResumeRepository
{
    Task<Resume?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Resume resume, CancellationToken cancellationToken = default);
    void Update(Resume resume);
    void Delete(Resume resume);
}

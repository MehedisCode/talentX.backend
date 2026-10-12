using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Update(Project project);
    void Delete(Project project);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

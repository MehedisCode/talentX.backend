using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IWorkExperienceRepository
{
    Task<IReadOnlyList<WorkExperience>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<WorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkExperience workExperience, CancellationToken cancellationToken = default);
    void Update(WorkExperience workExperience);
    void Delete(WorkExperience workExperience);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

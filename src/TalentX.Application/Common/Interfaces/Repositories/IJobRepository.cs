using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IJobRepository
{
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);
}

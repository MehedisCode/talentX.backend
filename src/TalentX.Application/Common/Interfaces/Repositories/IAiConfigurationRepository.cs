namespace TalentX.Application.Common.Interfaces.Repositories;

using TalentX.Domain.Entities;

public interface IAiConfigurationRepository
{
    Task<AiConfiguration?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AiConfiguration config, CancellationToken cancellationToken = default);
    void Update(AiConfiguration config);
}

using TalentX.Domain;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IInterviewPreparationConfigRepository
{
    Task<InterviewPreparationConfig?> GetAsync(CancellationToken ct = default);
    Task AddAsync(InterviewPreparationConfig config, CancellationToken ct = default);
    void Update(InterviewPreparationConfig config);
}

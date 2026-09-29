using TalentX.Domain;

namespace TalentX.Application.Common.Interfaces;

public interface IInterviewPreparationConfigProvider
{
    Task<InterviewPreparationConfig> GetConfigAsync(CancellationToken ct = default);
}

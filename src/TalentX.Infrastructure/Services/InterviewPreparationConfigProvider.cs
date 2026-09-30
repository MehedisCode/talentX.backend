using Microsoft.Extensions.Options;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Domain;

namespace TalentX.Infrastructure.Services;

public sealed class InterviewPreparationConfigProvider : IInterviewPreparationConfigProvider
{
    private readonly IInterviewPreparationConfigRepository _repository;
    private readonly InterviewPreparationSettings _fallback;

    public InterviewPreparationConfigProvider(
        IInterviewPreparationConfigRepository repository,
        IOptions<InterviewPreparationSettings> fallbackSettings)
    {
        _repository = repository;
        _fallback = fallbackSettings.Value;
    }

    public async Task<InterviewPreparationConfig> GetConfigAsync(CancellationToken ct = default)
    {
        var dbConfig = await _repository.GetAsync(ct);
        if (dbConfig is not null) return dbConfig;

        return InterviewPreparationConfig.CreateDefault(
            _fallback.DailyGenerateLimit,
            _fallback.DailyGenerationLimit,
            _fallback.MaxRegenerationCount,
            _fallback.AccessWindowAfterInterviewMinutes);
    }
}

using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationConfig;

public sealed class GetInterviewPreparationConfigQueryHandler
    : IRequestHandler<GetInterviewPreparationConfigQuery, InterviewPreparationConfigDto>
{
    private readonly IInterviewPreparationConfigProvider _configProvider;

    public GetInterviewPreparationConfigQueryHandler(
        IInterviewPreparationConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    public async Task<InterviewPreparationConfigDto> Handle(
        GetInterviewPreparationConfigQuery request, CancellationToken ct)
    {
        var config = await _configProvider.GetConfigAsync(ct);

        return new InterviewPreparationConfigDto(
            config.Id,
            config.DailyGenerateLimit,
            config.DailyGenerationLimit,
            config.MaxRegenerationCount,
            config.AccessWindowAfterInterviewMinutes,
            config.CreatedAt,
            config.UpdatedAt);
    }
}

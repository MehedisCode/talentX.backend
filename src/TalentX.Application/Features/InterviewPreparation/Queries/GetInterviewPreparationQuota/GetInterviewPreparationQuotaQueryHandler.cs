using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationQuota;

public sealed class GetInterviewPreparationQuotaQueryHandler
    : IRequestHandler<GetInterviewPreparationQuotaQuery, InterviewPreparationQuotaDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationConfigProvider _configProvider;

    public GetInterviewPreparationQuotaQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationConfigProvider configProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _configProvider = configProvider;
    }

    public async Task<InterviewPreparationQuotaDto> Handle(
        GetInterviewPreparationQuotaQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var generatesUsed = await _unitOfWork.InterviewPreparations
            .CountInitialGenerationsTodayByTalentAsync(userId, ct);
        var generationsUsed = await _unitOfWork.InterviewPreparations
            .CountAllGenerationsTodayByTalentAsync(userId, ct);

        return new InterviewPreparationQuotaDto
        {
            DailyGenerateLimit = config.DailyGenerateLimit,
            DailyGenerationLimit = config.DailyGenerationLimit,
            MaxRegenerationCount = config.MaxRegenerationCount,
            AccessWindowAfterInterviewMinutes = config.AccessWindowAfterInterviewMinutes,
            GeneratesUsedToday = generatesUsed,
            GenerationsUsedToday = generationsUsed
        };
    }
}

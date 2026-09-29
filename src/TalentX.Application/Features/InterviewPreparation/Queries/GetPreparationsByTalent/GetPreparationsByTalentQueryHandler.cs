using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public sealed class GetPreparationsByTalentQueryHandler
    : IRequestHandler<GetPreparationsByTalentQuery, IReadOnlyList<InterviewPreparationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationConfigProvider _configProvider;

    public GetPreparationsByTalentQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationConfigProvider configProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _configProvider = configProvider;
    }

    public async Task<IReadOnlyList<InterviewPreparationDto>> Handle(
        GetPreparationsByTalentQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var preparations = await _unitOfWork.InterviewPreparations
            .GetByTalentUserIdAsync(userId, ct);

        var accessWindow = config.AccessWindowAfterInterview;

        return preparations
            .Select(p => InterviewPreparationMapper.ToDto(p, accessWindow))
            .ToList();
    }
}

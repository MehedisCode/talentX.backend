using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Helpers;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;

public sealed class GetPreparationQueryHandler
    : IRequestHandler<GetPreparationQuery, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationConfigProvider _configProvider;

    public GetPreparationQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationConfigProvider configProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _configProvider = configProvider;
    }

    public async Task<InterviewPreparationDto> Handle(
        GetPreparationQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var preparation = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        InterviewPreparationAccessValidator.EnsureUserCanAccess(preparation, userId);

        return InterviewPreparationMapper.ToDto(
            preparation, config.AccessWindowAfterInterview);
    }
}

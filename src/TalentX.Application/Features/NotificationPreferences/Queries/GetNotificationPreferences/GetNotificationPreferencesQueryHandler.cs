using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.NotificationPreferences.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, TalentNotificationPreferenceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationPreferencesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<TalentNotificationPreferenceDto> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view notification preferences.");

        var preference = await _unitOfWork.TalentNotificationPreferences.GetByTalentIdAsync(talentId, cancellationToken);

        return preference is not null
            ? new TalentNotificationPreferenceDto(preference.JobAlertsEmailEnabled)
            : new TalentNotificationPreferenceDto(JobAlertsEmailEnabled: true);
    }
}

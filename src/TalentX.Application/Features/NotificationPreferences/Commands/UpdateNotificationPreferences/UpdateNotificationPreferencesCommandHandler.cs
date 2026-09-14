using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.NotificationPreferences.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateNotificationPreferencesCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update notification preferences.");

        var preference = await _unitOfWork.TalentNotificationPreferences.GetByTalentIdAsync(talentId, cancellationToken);
        if (preference is null)
        {
            preference = TalentNotificationPreference.CreateDefault(talentId);
            preference.Update(request.JobAlertsEmailEnabled);
            await _unitOfWork.TalentNotificationPreferences.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.Update(request.JobAlertsEmailEnabled);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

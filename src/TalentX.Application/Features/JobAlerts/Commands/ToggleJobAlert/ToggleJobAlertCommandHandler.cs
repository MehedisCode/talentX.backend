using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;

public sealed class ToggleJobAlertCommandHandler : IRequestHandler<ToggleJobAlertCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ToggleJobAlertCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        ToggleJobAlertCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to toggle a job alert.");

        var alert = await _unitOfWork.JobAlerts.GetByIdAsync(request.Id, cancellationToken);
        if (alert is null)
        {
            throw new NotFoundException("Job alert not found.");
        }

        if (alert.TalentId != talentId)
        {
            throw new ForbiddenException("You do not have permission to modify this job alert.");
        }

        alert.Toggle(request.IsEnabled);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;

public sealed class DeleteJobAlertCommandHandler : IRequestHandler<DeleteJobAlertCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteJobAlertCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        DeleteJobAlertCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to delete a job alert.");

        var alert = await _unitOfWork.JobAlerts.GetByIdAsync(request.Id, cancellationToken);
        if (alert is null)
        {
            throw new NotFoundException("Job alert not found.");
        }

        if (alert.TalentId != talentId)
        {
            throw new ForbiddenException("You do not have permission to delete this job alert.");
        }

        _unitOfWork.JobAlerts.Remove(alert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

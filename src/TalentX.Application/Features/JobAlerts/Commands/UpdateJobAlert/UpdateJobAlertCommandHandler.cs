using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;

public sealed class UpdateJobAlertCommandHandler : IRequestHandler<UpdateJobAlertCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateJobAlertCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        UpdateJobAlertCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update a job alert.");

        var alert = await _unitOfWork.JobAlerts.GetByIdAsync(request.Id, cancellationToken);
        if (alert is null)
        {
            throw new NotFoundException("Job alert not found.");
        }

        if (alert.TalentId != talentId)
        {
            throw new ForbiddenException("You do not have permission to update this job alert.");
        }

        alert.Update(
            request.Parameters.Name,
            request.Parameters.Title,
            request.Parameters.Location,
            request.Parameters.EmploymentType,
            request.Parameters.ExperienceLevel,
            request.Parameters.WorkArrangement);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobAlerts.Constants;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;

public sealed class CreateJobAlertCommandHandler : IRequestHandler<CreateJobAlertCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateJobAlertCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateJobAlertCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to create a job alert.");

        var alertCount = await _unitOfWork.JobAlerts.CountByTalentIdAsync(talentId, cancellationToken);
        if (alertCount >= JobAlertConstants.MaxAlertsPerTalent)
        {
            throw new BadRequestException($"You cannot create more than {JobAlertConstants.MaxAlertsPerTalent} job alerts.");
        }

        var alert = JobAlert.Create(
            talentId,
            request.Parameters.Name,
            request.Parameters.Title,
            request.Parameters.Location,
            request.Parameters.EmploymentType,
            request.Parameters.ExperienceLevel,
            request.Parameters.WorkArrangement);

        await _unitOfWork.JobAlerts.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return alert.Id;
    }
}

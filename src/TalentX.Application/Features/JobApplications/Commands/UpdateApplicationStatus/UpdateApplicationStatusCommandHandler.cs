using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;

public sealed class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateApplicationStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to update an application status.");

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException($"Job application '{request.ApplicationId}' was not found.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(application.JobId, cancellationToken);
        if (job is null || job.EmployerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to update status for this job application.");
        }

        application.UpdateStatus(request.NewStatus);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

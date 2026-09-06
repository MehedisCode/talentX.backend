using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Commands.CloseJob;

public sealed class CloseJobCommandHandler : IRequestHandler<CloseJobCommand, Unit>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CloseJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(CloseJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to close a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job with ID '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to close this job.");
        }

        job.MarkAsClosed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

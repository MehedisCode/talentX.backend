using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Commands.DeleteJob;

public sealed class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand, Unit>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to delete a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job with ID '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to delete this job.");
        }

        job.SoftDelete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

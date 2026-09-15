using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Commands.ReopenJob;

public sealed class ReopenJobCommandHandler : IRequestHandler<ReopenJobCommand, Unit>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobAlertQueue _jobAlertQueue;
    private readonly ILogger<ReopenJobCommandHandler> _logger;

    public ReopenJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobAlertQueue jobAlertQueue,
        ILogger<ReopenJobCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobAlertQueue = jobAlertQueue;
        _logger = logger;
    }

    public async Task<Unit> Handle(ReopenJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to reopen a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job with ID '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to reopen this job.");
        }

        try
        {
            job.Reopen();
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _jobAlertQueue.EnqueuePublishedJobAsync(job.Id, cancellationToken);
        }
        catch (Exception alertException)
        {
            _logger.LogError(alertException, "Failed to enqueue reopened job {JobId} for job alerts", job.Id);
        }

        return Unit.Value;
    }
}

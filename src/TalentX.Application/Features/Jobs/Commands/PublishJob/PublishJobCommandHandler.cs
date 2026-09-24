using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.PublishJob;

public sealed class PublishJobCommandHandler : IRequestHandler<PublishJobCommand, Unit>
{
    private readonly IJobRepository _jobRepository;
    private readonly IEmployerRepository _employerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobAlertQueue _jobAlertQueue;

    public PublishJobCommandHandler(
        IJobRepository jobRepository,
        IEmployerRepository employerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobAlertQueue jobAlertQueue)
    {
        _jobRepository = jobRepository;
        _employerRepository = employerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobAlertQueue = jobAlertQueue;
    }

    public async Task<Unit> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to publish a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job with ID '{request.Id}' was not found.");

        if (job.EmployerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to publish this job.");
        }

        var employer = await _employerRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Employer profile not found.");

        if (employer.VerificationStatus != EmployerVerificationStatus.Verified)
        {
            throw new ForbiddenException("Only verified employers can publish jobs. Your account is pending verification.");
        }

        try
        {
            JobRules.EnsurePublishable(job);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        job.MarkAsPublished();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _jobAlertQueue.EnqueuePublishedJobAsync(job.Id, cancellationToken);

        return Unit.Value;
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Jobs.Commands.DeleteJob;

public sealed class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand, Unit>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DeleteJobCommandHandler> _logger;

    public DeleteJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        ILogger<DeleteJobCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _logger = logger;
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

        // The soft-deleted job's applications no longer keep their resume files
        // alive; remove any object that is not referenced by another active
        // application or a talent's profile library. Best-effort: the job is
        // already removed and a storage failure must not fail this command.
        await CleanupApplicationResumeFilesAsync(job.Id, cancellationToken);

        return Unit.Value;
    }

    private async Task CleanupApplicationResumeFilesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var applications = await _unitOfWork.JobApplications.GetByJobIdAsync(jobId, cancellationToken);

        var storageKeys = applications
            .Select(application => application.ResumeStorageKey)
            .Where(storageKey => !string.IsNullOrWhiteSpace(storageKey))
            .Distinct()
            .ToList();

        foreach (var storageKey in storageKeys)
        {
            try
            {
                await ResumeFileCleanup.TryDeleteIfUnreferencedAsync(
                    _unitOfWork,
                    _fileStorageService,
                    storageKey,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to clean up resume {StorageKey} after deleting job {JobId}",
                    storageKey,
                    jobId);
            }
        }
    }
}

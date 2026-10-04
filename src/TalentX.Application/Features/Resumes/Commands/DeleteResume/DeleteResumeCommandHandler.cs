using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Resumes.Commands.DeleteResume;

public sealed class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteResumeCommandHandler> _logger;

    public DeleteResumeCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<DeleteResumeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var resumes = await _unitOfWork.Resumes.GetAllByUserIdAsync(userId, cancellationToken);
        var resume = resumes.FirstOrDefault(item => item.Id == request.Id);
        if (resume is null)
            throw new NotFoundException($"Resume '{request.Id}' was not found.");

        _unitOfWork.Resumes.Delete(resume);

        if (resume.IsDefault)
        {
            var replacement = resumes
                .Where(item => item.Id != resume.Id)
                .OrderByDescending(item => item.UploadedAt)
                .FirstOrDefault();

            replacement?.SetDefault();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The stored object is shared with any applications that used this resume.
        // Deleting the profile row is always allowed; the file survives while an
        // active job application still references it and is cleaned up when that
        // job is removed.
        try
        {
            await ResumeFileCleanup.TryDeleteIfUnreferencedAsync(
                _unitOfWork,
                _fileStorageService,
                resume.StorageKey,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to delete stored file {StorageKey} after deleting resume {ResumeId}",
                resume.StorageKey,
                resume.Id);
        }
    }
}

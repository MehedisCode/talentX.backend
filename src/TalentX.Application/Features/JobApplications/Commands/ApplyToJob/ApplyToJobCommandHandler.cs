using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.JobApplications.Commands.ApplyToJob;

public sealed class ApplyToJobCommandHandler
    : IRequestHandler<ApplyToJobCommand, ApplicationSubmittedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ApplyToJobCommandHandler> _logger;

    public ApplyToJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        ILogger<ApplyToJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ApplicationSubmittedDto> Handle(
        ApplyToJobCommand request,
        CancellationToken cancellationToken)
    {
        var talentUserId = _currentUserService.UserId!;

        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("This job is no longer open for applications.");

        if (await _unitOfWork.JobApplications.ExistsByJobIdAndTalentUserIdAsync(job.Id, talentUserId, cancellationToken))
            throw new ConflictException("You have already applied to this job.");

        var application = new JobApplication(
            job.Id,
            talentUserId,
            JobApplicationRules.NormalizeRequired(request.ApplicantName),
            JobApplicationRules.NormalizeRequired(request.ApplicantEmail),
            JobApplicationRules.NormalizeRequired(request.ResumeStorageKey),
            JobApplicationRules.NormalizeOptional(request.CoverLetter),
            request.ExpectedSalary,
            request.TotalYearsExperience,
            JobApplicationRules.NormalizeOptional(request.Location));

        await _unitOfWork.JobApplications.AddAsync(application, cancellationToken);
        await _unitOfWork.JobApplicationStatusHistories.AddAsync(
            JobApplicationStatusHistory.Create(application.Id, null, application.Status, talentUserId),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await CleanupPendingGuestApplicationsAsync(job.Id, request.ApplicantEmail, cancellationToken);

        return new ApplicationSubmittedDto(
            application.Id,
            application.JobId,
            application.Status.ToString(),
            application.AppliedAt);
    }

    private async Task CleanupPendingGuestApplicationsAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken)
    {
        var pendingGuest = await _unitOfWork.JobApplications
            .GetPendingByJobIdAndEmailAsync(jobId, email, cancellationToken);

        if (pendingGuest is null)
            return;

        if (!string.IsNullOrWhiteSpace(pendingGuest.ResumeStorageKey))
        {
            try
            {
                await _fileStorageService.DeleteAsync(pendingGuest.ResumeStorageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up guest resume {StorageKey}", pendingGuest.ResumeStorageKey);
            }
        }

        _unitOfWork.JobApplications.Delete(pendingGuest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

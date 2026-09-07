using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;

public sealed class GetJobApplicationByIdQueryHandler
    : IRequestHandler<GetJobApplicationByIdQuery, JobApplicationDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPresignedUrlService _presignedUrlService;

    // Use a 1-hour expiry for the presigned URL
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromHours(1);

    public GetJobApplicationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<JobApplicationDetailsDto> Handle(
        GetJobApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Application '{request.Id}' was not found.");

    
        if (application.TalentUserId != userId)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(application.JobId, cancellationToken);
            if (job is null || job.EmployerId != userId)
                throw new ForbiddenException("You do not have permission to view this application.");
        }

        string? resumeDownloadUrl = null;
        if (!string.IsNullOrWhiteSpace(application.ResumeStorageKey))
        {
            resumeDownloadUrl = await _presignedUrlService.CreateDownloadUrlAsync(
                application.ResumeStorageKey,
                DownloadUrlExpiry,
                cancellationToken);
        }

        return new JobApplicationDetailsDto(
            application.Id,
            application.JobId,
            application.ApplicantName,
            application.ApplicantEmail,
            application.ResumeStorageKey,
            resumeDownloadUrl,
            application.CoverLetter,
            application.ExpectedSalary,
            application.TotalYearsExperience,
            application.Location,
            application.Status.ToString(),
            application.AiMatchScore,
            application.AppliedAt,
            application.UpdatedAt);
    }
}

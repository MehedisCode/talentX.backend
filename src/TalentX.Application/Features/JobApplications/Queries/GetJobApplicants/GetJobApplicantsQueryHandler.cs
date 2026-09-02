using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;

public sealed class GetJobApplicantsQueryHandler
    : IRequestHandler<GetJobApplicantsQuery, IReadOnlyList<JobApplicantListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPresignedUrlService _presignedUrlService;

    private static readonly TimeSpan PresignedUrlExpiry = TimeSpan.FromHours(1);

    public GetJobApplicantsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<IReadOnlyList<JobApplicantListItemDto>> Handle(
        GetJobApplicantsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to view job applicants.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view applicants for this job.");
        }

        var applications = await _unitOfWork.JobApplications.GetByJobIdAsync(request.JobId, cancellationToken);

        var tasks = applications
            .OrderByDescending(app => app.AppliedAt)
            .Select(async app =>
            {
                string? resumeDownloadUrl = null;
                if (!string.IsNullOrWhiteSpace(app.ResumeStorageKey))
                {
                    resumeDownloadUrl = await _presignedUrlService.CreateDownloadUrlAsync(
                        app.ResumeStorageKey,
                        PresignedUrlExpiry,
                        cancellationToken);
                }

                return new JobApplicantListItemDto(
                    app.Id,
                    app.JobId,
                    app.TalentUserId,
                    app.ApplicantName,
                    app.ApplicantEmail,
                    resumeDownloadUrl,
                    app.Status.ToString(),
                    app.AiMatchScore,
                    app.ExpectedSalary,
                    app.TotalYearsExperience,
                    app.Location,
                    app.AppliedAt);
            });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}

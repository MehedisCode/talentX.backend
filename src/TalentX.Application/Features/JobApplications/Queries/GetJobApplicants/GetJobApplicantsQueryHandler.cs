using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;

public sealed class GetJobApplicantsQueryHandler
    : IRequestHandler<GetJobApplicantsQuery, PaginatedList<JobApplicantListItemDto>>
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

    public async Task<PaginatedList<JobApplicantListItemDto>> Handle(
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

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var paginatedApplications = await _unitOfWork.JobApplications.GetByJobIdPaginatedAsync(
            request.JobId,
            request.Search,
            request.Status,
            request.SortBy,
            request.SortOrder,
            page,
            pageSize,
            cancellationToken);

        var urlTasks = paginatedApplications.Items
            .Select(app => string.IsNullOrWhiteSpace(app.ResumeStorageKey)
                ? Task.FromResult<string?>(null)
                : _presignedUrlService.CreateDownloadUrlAsync(app.ResumeStorageKey, PresignedUrlExpiry, cancellationToken))
            .ToArray();

        var resumeUrls = await Task.WhenAll(urlTasks);

        var results = paginatedApplications.Items
            .Select((app, index) => new JobApplicantListItemDto(
                app.Id,
                app.JobId,
                app.TalentUserId,
                app.ApplicantName,
                app.ApplicantEmail,
                resumeUrls[index],
                app.Status.ToString(),
                app.AiMatchScore,
                app.ExpectedSalary,
                app.TotalYearsExperience,
                app.Location,
                app.AppliedAt))
            .ToList();

        return new PaginatedList<JobApplicantListItemDto>(
            results,
            paginatedApplications.Page,
            paginatedApplications.PageSize,
            paginatedApplications.TotalCount);
    }
}

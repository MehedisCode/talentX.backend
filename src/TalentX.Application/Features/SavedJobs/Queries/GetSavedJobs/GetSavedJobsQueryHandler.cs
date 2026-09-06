using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.SavedJobs.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.SavedJobs.Queries.GetSavedJobs;

public sealed class GetSavedJobsQueryHandler
    : IRequestHandler<GetSavedJobsQuery, PaginatedList<SavedJobResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetSavedJobsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<SavedJobResponse>> Handle(
        GetSavedJobsQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view saved jobs.");

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var savedJobs = await _unitOfWork.SavedJobs.GetSavedJobsAsync(talentId, page, pageSize, cancellationToken);

        var jobIds = savedJobs.Items.Select(sj => sj.JobId).ToList();

        var jobs = (await _unitOfWork.Jobs.GetByIdsAsync(jobIds, cancellationToken))
            .ToDictionary(j => j.Id);

        var items = savedJobs.Items.Select(savedJob =>
        {
            jobs.TryGetValue(savedJob.JobId, out var job);

            return new SavedJobResponse
            {
                SavedJobId = savedJob.Id,
                JobId = savedJob.JobId,
                JobTitle = job?.Title ?? string.Empty,
                CompanyName = job?.CompanyName ?? string.Empty,
                Location = job?.Location ?? string.Empty,
                EmploymentType = job?.EmploymentType.ToString() ?? string.Empty,
                PostedDate = job?.CreatedAt ?? default,
                IsAvailable = job is not null
                    && !job.IsDeleted
                    && job.Status == JobStatus.Published
                    && job.ExpireAt >= DateTime.UtcNow
            };
        }).ToList();

        return new PaginatedList<SavedJobResponse>(items, savedJobs.Page, savedJobs.PageSize, savedJobs.TotalCount);
    }
}

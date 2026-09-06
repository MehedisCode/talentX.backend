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

        var items = new List<SavedJobResponse>();

        foreach (var savedJob in savedJobs.Items)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(savedJob.JobId, cancellationToken);

            items.Add(new SavedJobResponse
            {
                SavedJobId = savedJob.Id,
                JobId = savedJob.JobId,
                JobTitle = job?.Title ?? string.Empty,
                CompanyName = job?.CompanyName ?? string.Empty,
                Location = job?.Location ?? string.Empty,
                EmploymentType = job?.EmploymentType.ToString() ?? string.Empty,
                CreatedAt = savedJob.CreatedAt,
                IsAvailable = job is not null
                    && job.Status == JobStatus.Published
                    && !job.IsDeleted
                    && job.ExpireAt >= DateTime.UtcNow
            });
        }

        return new PaginatedList<SavedJobResponse>(items, savedJobs.Page, savedJobs.PageSize, savedJobs.TotalCount);
    }
}

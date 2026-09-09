using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.SavedJobs.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.SavedJobs.Queries.GetSavedJobs;

public sealed class GetSavedJobsQueryHandler
    : IRequestHandler<GetSavedJobsQuery, PaginatedList<SavedJobResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetSavedJobsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
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

            var response = job is not null
                ? _mapper.Map<SavedJobResponse>(job)
                : new SavedJobResponse { IsAvailable = false };

            response.SavedJobId = savedJob.Id;
            response.JobId = savedJob.JobId;
            return response;
        }).ToList();

        return new PaginatedList<SavedJobResponse>(items, savedJobs.Page, savedJobs.PageSize, savedJobs.TotalCount);
    }
}

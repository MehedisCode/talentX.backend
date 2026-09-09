using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerJobs;

public sealed class GetEmployerJobsQueryHandler : IRequestHandler<GetEmployerJobsQuery, PaginatedList<EmployerJobListItemDto>>
{
    private readonly IJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployerJobsQueryHandler(
        IJobRepository jobRepository,
        ICurrentUserService currentUserService)
    {
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<EmployerJobListItemDto>> Handle(
        GetEmployerJobsQuery request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your jobs.");

        var (normalizedPage, normalizedPageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        return await _jobRepository.GetEmployerJobsPaginatedAsync(
            employerId,
            request.Status,
            request.IsExpired,
            normalizedPage,
            normalizedPageSize,
            request.SearchTerm,
            cancellationToken);
    }
}

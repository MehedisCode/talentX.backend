using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobAlerts.Extensions;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;

public sealed class GetPreferredJobsQueryHandler : IRequestHandler<GetPreferredJobsQuery, PaginatedList<JobListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetPreferredJobsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PaginatedList<JobListItemDto>> Handle(
        GetPreferredJobsQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view preferred jobs.");

        var (page, pageSize) = PaginationRequest.Normalize(request.Parameters.Page, request.Parameters.PageSize);

        List<JobAlert> alertsToMatch;

        if (request.Parameters.AlertId.HasValue)
        {
            var alert = await _unitOfWork.JobAlerts.GetByIdAsync(request.Parameters.AlertId.Value, cancellationToken);
            if (alert is null)
            {
                throw new NotFoundException("Job alert not found.");
            }

            if (alert.TalentId != talentId)
            {
                throw new ForbiddenException("You do not have permission to view jobs for this alert.");
            }

            alertsToMatch = [alert];
        }
        else
        {
            alertsToMatch = await _unitOfWork.JobAlerts.GetActiveAlertsByTalentIdAsync(talentId, cancellationToken);
        }

        if (alertsToMatch.Count == 0)
        {
            return new PaginatedList<JobListItemDto>([], page, pageSize, 0);
        }

        var openJobs = (await _unitOfWork.Jobs.GetActivePublishedJobsAsync(cancellationToken)).ToList();

        var matchedJobs = alertsToMatch
            .SelectMany(alert => openJobs.ApplyAlertCriteria(alert))
            .GroupBy(job => job.Id)
            .Select(group => group.First())
            .OrderByDescending(job => job.PublishedAt ?? job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .ToList();

        var totalCount = matchedJobs.Count;

        var pagedJobs = matchedJobs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = _mapper.Map<List<JobListItemDto>>(pagedJobs);

        return new PaginatedList<JobListItemDto>(items, page, pageSize, totalCount);
    }
}

using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplications;

public sealed class GetTalentApplicationsQueryHandler
    : IRequestHandler<GetTalentApplicationsQuery, PaginatedList<TalentApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTalentApplicationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<TalentApplicationDto>> Handle(
        GetTalentApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to access your applications.");

        ApplicationStatus? filterStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<ApplicationStatus>(request.Status, true, out var parsedStatus))
            {
                filterStatus = parsedStatus;
            }
            else
            {
                throw new BadRequestException($"Invalid status filter '{request.Status}'. Allowed values are Applied, Reviewed, Interview, Hired, Rejected.");
            }
        }

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var pagedTalentApplications = await _unitOfWork.JobApplications.GetApplicationsForTalentAsync(
            currentUserId,
            filterStatus,
            page,
            pageSize,
            cancellationToken);

        var talentApplications = pagedTalentApplications.Items.Select(r => new TalentApplicationDto(
            r.Id,
            r.JobId,
            r.JobTitle,
            r.Company,
            r.AppliedDate,
            r.Status.ToString()
        )).ToList();

        return new PaginatedList<TalentApplicationDto>(talentApplications, pagedTalentApplications.Page, pagedTalentApplications.PageSize, pagedTalentApplications.TotalCount);
    }
}

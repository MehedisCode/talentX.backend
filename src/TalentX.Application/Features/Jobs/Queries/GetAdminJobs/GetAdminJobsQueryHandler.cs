using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetAdminJobs;

public sealed class GetAdminJobsQueryHandler : IRequestHandler<GetAdminJobsQuery, PaginatedList<AdminJobListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<AdminJobListItemDto>> Handle(GetAdminJobsQuery request, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var queryParams = new AdminJobQueryParams
        {
            SearchQuery = request.SearchQuery,
            Status = request.Status,
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };

        return await _unitOfWork.Jobs.GetAdminJobsPagedAsync(queryParams, cancellationToken);
    }
}

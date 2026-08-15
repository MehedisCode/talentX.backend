using MediatR;
using TalentX.Application.Common;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Jobs;
using TalentX.Application.Mappings;

namespace TalentX.Application.Features.Jobs.Queries;

public record GetPublishedJobsQuery(int Page, int PageSize) : IRequest<PagedResult<JobListItemDto>>;

public class GetPublishedJobsQueryHandler : IRequestHandler<GetPublishedJobsQuery, PagedResult<JobListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublishedJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<JobListItemDto>> Handle(GetPublishedJobsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (jobs, totalCount) = await _unitOfWork.Jobs.GetPublishedJobsAsync(page, pageSize, cancellationToken);

        return new PagedResult<JobListItemDto>
        {
            Items = jobs.Select(j => j.ToListItemDto()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

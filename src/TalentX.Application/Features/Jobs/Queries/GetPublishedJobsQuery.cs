using AutoMapper;
using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries;

public record GetPublishedJobsQuery(int Page, int PageSize) : IRequest<PaginatedList<JobListItemDto>>;

public class GetPublishedJobsQueryHandler : IRequestHandler<GetPublishedJobsQuery, PaginatedList<JobListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublishedJobsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedList<JobListItemDto>> Handle(GetPublishedJobsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var jobs = await _unitOfWork.Jobs.GetPublishedJobsAsync(page, pageSize, cancellationToken);
        var items = _mapper.Map<List<JobListItemDto>>(jobs.Items);

        return new PaginatedList<JobListItemDto>(items, jobs.Page, jobs.PageSize, jobs.TotalCount);
    }
}

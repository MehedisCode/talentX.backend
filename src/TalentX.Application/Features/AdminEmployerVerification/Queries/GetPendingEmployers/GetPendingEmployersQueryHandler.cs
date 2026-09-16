using AutoMapper;
using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AdminEmployerVerification.DTOs;

namespace TalentX.Application.Features.AdminEmployerVerification.Queries.GetPendingEmployers;

public sealed class GetPendingEmployersQueryHandler
    : IRequestHandler<GetPendingEmployersQuery, PaginatedList<PendingEmployerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPendingEmployersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedList<PendingEmployerDto>> Handle(
        GetPendingEmployersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _unitOfWork.Employers.GetPagedAsync(
            request.SearchTerm,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = _mapper.Map<List<PendingEmployerDto>>(items);

        return new PaginatedList<PendingEmployerDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}

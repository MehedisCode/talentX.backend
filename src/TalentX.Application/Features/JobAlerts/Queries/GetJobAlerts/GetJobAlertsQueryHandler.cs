using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobAlerts.Queries.GetJobAlerts;

public sealed class GetJobAlertsQueryHandler : IRequestHandler<GetJobAlertsQuery, List<JobAlertDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetJobAlertsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<List<JobAlertDto>> Handle(
        GetJobAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view job alerts.");

        var alerts = await _unitOfWork.JobAlerts.GetByTalentIdAsync(talentId, cancellationToken);

        return _mapper.Map<List<JobAlertDto>>(alerts);
    }
}

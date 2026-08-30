using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;

public sealed class GetMyApplicationStatusQueryHandler
    : IRequestHandler<GetMyApplicationStatusQuery, MyApplicationStatusDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyApplicationStatusQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<MyApplicationStatusDto> Handle(
        GetMyApplicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var talentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to check application status.");

        // Only eligible jobs (Published and not expired) can be applied to, so an ineligible
        // or unknown job id is a 404 — consistent with GET /api/v1/Jobs/{id}.
        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        var hasApplied = await _unitOfWork.JobApplications
            .ExistsForJobAndTalentAsync(job.Id, talentUserId, cancellationToken);

        return new MyApplicationStatusDto(hasApplied);
    }
}

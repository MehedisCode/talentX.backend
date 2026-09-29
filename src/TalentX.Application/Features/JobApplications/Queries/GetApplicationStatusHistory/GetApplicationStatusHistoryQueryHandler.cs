using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Queries.GetApplicationStatusHistory;

public sealed class GetApplicationStatusHistoryQueryHandler
    : IRequestHandler<GetApplicationStatusHistoryQuery, IReadOnlyList<ApplicationStatusHistoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetApplicationStatusHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ApplicationStatusHistoryDto>> Handle(
        GetApplicationStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to view application status history.");

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException($"Job application '{request.ApplicationId}' was not found.");

        if (application.TalentUserId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view status history for this job application.");
        }

        var histories = await _unitOfWork.JobApplicationStatusHistories.GetByApplicationIdAsync(
            request.ApplicationId,
            cancellationToken);

        return histories
            .Select(history => new ApplicationStatusHistoryDto(
                history.FromStatus?.ToString(),
                history.ToStatus.ToString(),
                history.ChangedAt))
            .ToList();
    }
}

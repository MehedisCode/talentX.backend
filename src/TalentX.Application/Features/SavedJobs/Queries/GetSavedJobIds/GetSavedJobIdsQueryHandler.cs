using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.SavedJobs.Queries.GetSavedJobIds;

public sealed class GetSavedJobIdsQueryHandler
    : IRequestHandler<GetSavedJobIdsQuery, List<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetSavedJobIdsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<Guid>> Handle(
        GetSavedJobIdsQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view saved jobs.");

        return await _unitOfWork.SavedJobs.GetSavedJobIdsAsync(talentId, cancellationToken);
    }
}

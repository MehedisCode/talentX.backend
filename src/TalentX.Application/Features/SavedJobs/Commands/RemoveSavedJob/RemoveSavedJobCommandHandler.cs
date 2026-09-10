using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;

public sealed class RemoveSavedJobCommandHandler
    : IRequestHandler<RemoveSavedJobCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveSavedJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        RemoveSavedJobCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to remove saved jobs.");

        var savedJob = await _unitOfWork.SavedJobs.FindAsync(talentId, request.JobId, cancellationToken)
            ?? throw new NotFoundException("Saved job not found.");

        _unitOfWork.SavedJobs.Remove(savedJob);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

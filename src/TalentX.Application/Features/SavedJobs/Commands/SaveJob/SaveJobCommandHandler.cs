using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.SavedJobs.Commands.SaveJob;

public sealed class SaveJobCommandHandler
    : IRequestHandler<SaveJobCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        SaveJobCommand request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to save jobs.");

        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("This job is no longer available for saving.");

        var existing = await _unitOfWork.SavedJobs.FindAsync(talentId, job.Id, cancellationToken);
        if (existing is not null)
            throw new ConflictException("You have already saved this job.");

        var savedJob = SavedJob.Create(job.Id, talentId);

        await _unitOfWork.SavedJobs.AddAsync(savedJob, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return savedJob.Id;
    }
}

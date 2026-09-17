using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;

public sealed class ResubmitJobForReviewCommandHandler : IRequestHandler<ResubmitJobForReviewCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ResubmitJobForReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(ResubmitJobForReviewCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to resubmit a job.");
        var employerEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to resubmit a job.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Job", request.Id);

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to resubmit this job.");
        }

        // Checked before completeness so an incomplete job in the wrong state reports the real
        // problem ("not deactivated") instead of a misleading list of missing fields.
        if (job.Status != JobStatus.Deactivated)
        {
            throw new BadRequestException("Only deactivated jobs can be resubmitted for review.");
        }

        // Same completeness rules as publishing, so an unfinished job never reaches the admin's
        // review queue.
        try
        {
            JobRules.EnsureContentComplete(job);
        }
        catch (InvalidOperationException exception)
        {
            throw new FluentValidation.ValidationException(exception.Message);
        }

        var fromStatus = job.Status;
        job.SubmitForReview();

        await _unitOfWork.JobModerationHistories.AddAsync(
            JobModerationHistory.Create(
                job.Id,
                JobModerationAction.ResubmittedForReview,
                fromStatus,
                job.Status,
                employerId,
                employerEmail),
            cancellationToken);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(employerEmail, $"Resubmitted job '{job.Title}' for review"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

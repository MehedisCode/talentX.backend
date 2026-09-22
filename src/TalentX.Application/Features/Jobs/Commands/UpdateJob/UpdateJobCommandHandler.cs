using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobAlertQueue _jobAlertQueue;
    private readonly ILogger<UpdateJobCommandHandler> _logger;

    public UpdateJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobAlertQueue jobAlertQueue,
        ILogger<UpdateJobCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobAlertQueue = jobAlertQueue;
        _logger = logger;
    }

    
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to edit a job.");

        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        if (job.EmployerId != employerId)
            throw new ForbiddenException("You do not have permission to edit this job.");

        // Locked while an admin reviews it, so the job can't change after it entered the queue.
        if (job.Status == JobStatus.PendingReview)
            throw new BadRequestException("This job is under admin review and can't be edited until the review is complete.");

        // Editing a deactivated job is how the employer fixes it, but going live again needs admin
        // approval. Without this they'd get the generic "Only draft jobs can be published".
        if (request.Publish && job.Status == JobStatus.Deactivated)
            throw new BadRequestException("This job was deactivated by an admin. Save your changes and resubmit it for review instead of publishing.");

        var isCurrentlyExpired = job.GetEffectiveStatus() == JobStatus.Expired;
        if (isCurrentlyExpired)
        {
            job.MarkAsExpired();
        }

        job.SetDetails(
            request.Title != null ? JobRules.NormalizeText(request.Title) : job.Title,
            request.CompanyName != null ? JobRules.NormalizeText(request.CompanyName) : job.CompanyName,
            request.Description != null ? JobRules.NormalizeText(request.Description) : job.Description,
            request.EmploymentType ?? job.EmploymentType,
            request.ExperienceLevel ?? job.ExperienceLevel,
            request.Location != null ? JobRules.NormalizeText(request.Location) : job.Location,
            request.WorkArrangement ?? job.WorkArrangement,
            request.RequiredSkills != null ? JobRules.NormalizeSkills(request.RequiredSkills) : job.RequiredSkills,
            request.Vacancies ?? job.Vacancies,
            request.ExpireAt ?? job.ExpireAt
        );

        // A Fixed salary is stored as SalaryMin == SalaryMax (see SalaryType) —
        // the client only ever sends one amount for Fixed, in SalaryMin. Merge
        // against the job's existing salary state first (partial-PATCH safe),
        // then derive Max from the merged type/min, not just this request's.
        var effectiveSalaryType = request.SalaryType ?? job.SalaryType;
        var effectiveSalaryMin = request.SalaryMin ?? job.SalaryMin;
        var effectiveSalaryMax = effectiveSalaryType == Domain.Enums.SalaryType.Fixed
            ? effectiveSalaryMin
            : (request.SalaryMax ?? job.SalaryMax);

        job.UpdateWizardDetails(
            request.City != null ? JobRules.NormalizeText(request.City) : job.City,
            request.Country != null ? JobRules.NormalizeText(request.Country) : job.Country,
            request.MinOverlapHours ?? job.MinOverlapHours,
            job.Responsibilities,
            effectiveSalaryType,
            effectiveSalaryMin,
            effectiveSalaryMax,
            request.SalaryCurrency ?? job.SalaryCurrency,
            request.SalaryPeriod ?? job.SalaryPeriod,
            request.Requirements != null ? JobRules.NormalizeText(request.Requirements) : job.Requirements
        );

        if (request.Publish)
        {
            var employer = await _unitOfWork.Employers.GetByUserIdAsync(employerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Employer), employerId);

            if (employer.VerificationStatus != EmployerVerificationStatus.Verified)
                throw new ForbiddenException("Only verified employers can publish jobs. Your account is pending verification.");

            try
            {
                JobRules.EnsurePublishable(job);
                job.MarkAsPublished();
            }
            catch (InvalidOperationException exception)
            {
                throw new FluentValidation.ValidationException(exception.Message);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.Publish && job.Status == JobStatus.Published)
        {
            try
            {
                await _jobAlertQueue.EnqueuePublishedJobAsync(job.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to enqueue published job {JobId} for job alerts", job.Id);
            }
        }
    }
}

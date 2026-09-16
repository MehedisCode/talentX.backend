using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Guid>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobAlertQueue _jobAlertQueue;
    private readonly ILogger<CreateJobCommandHandler> _logger;

    public CreateJobCommandHandler(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobAlertQueue jobAlertQueue,
        ILogger<CreateJobCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobAlertQueue = jobAlertQueue;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to post a job.");

        var job = Job.Create(
            JobRules.NormalizeText(employerId),
            JobRules.NormalizeText(request.Title),
            JobRules.NormalizeText(request.CompanyName),
            JobRules.NormalizeText(request.Description),
            request.EmploymentType,
            request.ExperienceLevel,
            JobRules.NormalizeText(request.Location),
            request.WorkArrangement,
            JobRules.NormalizeSkills(request.RequiredSkills),
            request.Vacancies,
            request.ExpireAt
        );

        // A Fixed salary is stored as SalaryMin == SalaryMax (see SalaryType) —
        // the client only ever sends one amount for Fixed, in SalaryMin.
        var effectiveSalaryMax = request.SalaryType == Domain.Enums.SalaryType.Fixed
            ? request.SalaryMin
            : request.SalaryMax;

        job.UpdateWizardDetails(
            request.City != null ? JobRules.NormalizeText(request.City) : null,
            request.Country != null ? JobRules.NormalizeText(request.Country) : null,
            request.MinOverlapHours,
            null,
            request.SalaryType,
            request.SalaryMin,
            effectiveSalaryMax,
            request.SalaryCurrency,
            request.SalaryPeriod,
            request.Requirements != null ? JobRules.NormalizeText(request.Requirements) : null
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
            catch (InvalidOperationException ex)
            {
                throw new FluentValidation.ValidationException(ex.Message);
            }
        }

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (job.Status == JobStatus.Published)
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

        return job.Id;
    }
}

using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Security;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandHandler : IRequestHandler<ApplyToJobCommand, ApplyToJobResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly ITalentRepository _talentRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyToJobCommandHandler(
        IJobRepository jobRepository,
        ITalentRepository talentRepository,
        IJobApplicationRepository jobApplicationRepository,
        IIdentityService identityService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _talentRepository = talentRepository;
        _jobApplicationRepository = jobApplicationRepository;
        _identityService = identityService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyToJobResponse> Handle(ApplyToJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetPublishedJobByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found or is no longer available.");

        if (job.WorkArrangement != WorkArrangement.RemoteButAccessible && string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ValidationException("Location is required for non-remote jobs.");
        }

        var name = request.ApplicantName.Trim();
        var email = request.ApplicantEmail.Trim().ToLowerInvariant();

        var existingTalent = await _talentRepository.GetByEmailAsync(email, cancellationToken);

        string? temporaryPassword = null;
        string talentUserId;
        var talentAccountCreated = false;

        if (existingTalent is not null)
        {
            talentUserId = existingTalent.UserId;

            if (await _jobApplicationRepository.ExistsForJobAndTalentAsync(request.JobId, talentUserId, cancellationToken))
            {
                throw new ConflictException("You have already applied for this job.");
            }
        }
        else
        {
            temporaryPassword = TemporaryPasswordGenerator.Generate();

            var createResult = await _identityService.CreateApplicationUserAsync(email, temporaryPassword, UserRole.Guest);
            if (!createResult.Success || createResult.Data is null)
            {
                throw new ConflictException(
                    $"An account is already registered with email '{email}'. Please sign in and apply instead.");
            }

            talentUserId = createResult.Data.UserId;
            await _talentRepository.AddAsync(new Talent(talentUserId, name, email), cancellationToken);
            talentAccountCreated = true;
        }

        var application = new JobApplication(
            request.JobId,
            talentUserId,
            name,
            email,
            request.ResumeStorageKey,
            request.CoverLetter,
            request.ExpectedSalary,
            request.TotalYearsExperience,
            request.Location);

        await _jobApplicationRepository.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (talentAccountCreated && temporaryPassword is not null)
        {
            await _emailService.SendCredentialsEmailAsync(email, name, temporaryPassword, cancellationToken);
        }

        return new ApplyToJobResponse(application.Id, application.JobId, talentAccountCreated);
    }
}
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents;
using TalentX.Domain;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandHandler : IRequestHandler<ApplyToJobCommand, ApplyToJobResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyToJobCommandHandler> _logger;

    public ApplyToJobCommandHandler(
        IJobRepository jobRepository,
        IJobApplicationRepository jobApplicationRepository,
        IEmailVerificationTokenRepository verificationTokenRepository,
        IEmailService emailService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ILogger<ApplyToJobCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _jobApplicationRepository = jobApplicationRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _emailService = emailService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

        var accountExists = await _unitOfWork.Users.ExistsByEmailAsync(email, cancellationToken);
        if (accountExists)
        {
            return new ApplyToJobResponse(Guid.Empty, request.JobId, AccountExists: true);
        }

        var existingApplication = await _jobApplicationRepository.GetAnyByJobIdAndEmailAsync(request.JobId, email, cancellationToken);

        if (existingApplication is not null)
        {
            if (existingApplication.Status == Domain.Enums.ApplicationStatus.Applied)
            {
                return new ApplyToJobResponse(existingApplication.Id, request.JobId, AlreadyApplied: true);
            }

            if (existingApplication.Status == Domain.Enums.ApplicationStatus.PendingVerification)
            {
                var newToken = GenerateSecureToken();
                var resendTokenEntity = new EmailVerificationToken(
                    newToken,
                    email,
                    existingApplication.Id,
                    TimeSpan.FromMinutes(30));

                await _verificationTokenRepository.AddAsync(resendTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var baseUrl = GetFrontendBaseUrl();
                var newUrl = $"{baseUrl}/verify-application?token={newToken}";
                _logger.LogInformation("[VERIFY] Verification link for {Email}: {Url}", email, newUrl);
                await _emailService.SendVerificationLinkEmailAsync(email, name, newUrl, cancellationToken);

                return new ApplyToJobResponse(existingApplication.Id, existingApplication.JobId, VerificationEmailResent: true);
            }
        }

        string resumeStorageKey = string.Empty;
        if (request.ResumeFile is not null)
        {
            var sanitizedResumeFileName = TalentMediaStorageKeyGuard.SanitizeFileNameSegment(request.ResumeFile.FileName);
            var storageFileName = TalentMediaStorageKeyGuard.BuildStorageKey(
                "resumes",
                "pending",
                $"{Guid.NewGuid()}_{sanitizedResumeFileName}");

            try
            {
                resumeStorageKey = await _fileStorageService.SaveAsync(request.ResumeFile, storageFileName, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to save resume {FileName} to storage", sanitizedResumeFileName);
                throw new BadGatewayException("File storage is temporarily unavailable.");
            }
        }

        var application = JobApplication.CreateGuestApplication(
            request.JobId,
            name,
            email,
            resumeStorageKey,
            coverLetter: request.CoverLetter?.Replace("\r\n", "\n").Replace("\r", "\n"),
            expectedSalary: request.ExpectedSalary,
            totalYearsExperience: request.TotalYearsExperience,
            location: request.Location,
            extractedTechnicalSkillsJson: request.ExtractedTechnicalSkillsJson,
            extractedSoftSkillsJson: request.ExtractedSoftSkillsJson,
            extractedToolsAndPlatformsJson: request.ExtractedToolsAndPlatformsJson,
            extractedWorkExperienceJson: request.ExtractedWorkExperienceJson,
            extractedEducationJson: request.ExtractedEducationJson,
            extractedProjectsJson: request.ExtractedProjectsJson,
            extractedProfileJson: request.ExtractedProfileJson);

        await _jobApplicationRepository.AddAsync(application, cancellationToken);
        await _unitOfWork.JobApplicationStatusHistories.AddAsync(
            JobApplicationStatusHistory.Create(application.Id, null, application.Status, email),
            cancellationToken);

        var verificationToken = GenerateSecureToken();
        var tokenEntity = new EmailVerificationToken(
            verificationToken,
            email,
            application.Id,
            TimeSpan.FromMinutes(30));

        await _verificationTokenRepository.AddAsync(tokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = GetFrontendBaseUrl();
        var verificationUrl = $"{frontendBaseUrl}/verify-application?token={verificationToken}";
        _logger.LogInformation("[VERIFY] Verification link for {Email}: {Url}", email, verificationUrl);
        await _emailService.SendVerificationLinkEmailAsync(email, name, verificationUrl, cancellationToken);

        return new ApplyToJobResponse(application.Id, application.JobId);
    }

    private static string GetFrontendBaseUrl()
    {
        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            var firstOrigin = corsOrigins
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstOrigin))
            {
                return firstOrigin.TrimEnd('/');
            }
        }

        return "http://localhost:3000";
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Helpers;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobApplications.Commands.ScoreJobApplicants;

public sealed class ScoreJobApplicantsCommandHandler
    : IRequestHandler<ScoreJobApplicantsCommand, ScoreJobApplicantsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITalentMatchingService _matchingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ScoreJobApplicantsCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public ScoreJobApplicantsCommandHandler(
        IUnitOfWork unitOfWork,
        ITalentMatchingService matchingService,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        ILogger<ScoreJobApplicantsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ScoreJobApplicantsResponse> Handle(
        ScoreJobApplicantsCommand request,
        CancellationToken cancellationToken)
    {
        var batchSize = MatchingBatch.ResolveSize(_configuration);
        var job = await GetAndValidateJobAsync(request.JobId, cancellationToken);

        var applications = (await _unitOfWork.JobApplications
            .GetByJobIdAsync(job.Id, cancellationToken))
            .Where(application => application.Status != ApplicationStatus.PendingVerification)
            .ToList();

        if (applications.Count == 0)
        {
            return new ScoreJobApplicantsResponse(job.Id, 0);
        }

        var jobProfile = BuildJobProfile(job);

        var candidates = new List<CandidateInput>();
        foreach (var application in applications)
        {
            var profile = await BuildCandidateProfileAsync(application, cancellationToken);
            candidates.Add(new CandidateInput(
                application,
                profile,
                MatchingInputHash.Compute(jobProfile, profile)));
        }

        var toScore = candidates
            .Where(c => c.Application.AiMatchScore is null
                || !string.Equals(c.Application.AiScoreInputHash, c.Hash, StringComparison.Ordinal))
            .ToList();
        var reusedCount = candidates.Count - toScore.Count;

        if (toScore.Count == 0)
        {
            _logger.LogInformation(
                "All {Count} applicants for job {JobId} reused existing AI scores",
                reusedCount, job.Id);
            return new ScoreJobApplicantsResponse(job.Id, 0, reusedCount);
        }

        var scored = 0;
        foreach (var batch in toScore.Chunk(batchSize))
        {
            var matchingRequest = new TalentMatchingRequest
            {
                Job = jobProfile,
                Candidates = batch.Select(c => c.Profile).ToList()
            };

            TalentMatchingResult matchResult;
            try
            {
                matchResult = await _matchingService.MatchAsync(matchingRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI applicant scoring failed for job {JobId} after {ScoredCount} of {PendingCount} applicants",
                    job.Id, scored, toScore.Count);
                throw new BadGatewayException("AI service is temporarily unavailable.");
            }

            var matchMap = BuildMatchMap(matchResult.Matches);

            foreach (var candidate in batch)
            {
                var key = candidate.Application.TalentUserId ?? $"guest-{candidate.Application.Id}";
                if (matchMap.TryGetValue(key, out var match))
                {
                    candidate.Application.SetAiMatchScore(match.Score, candidate.Hash);
                    _unitOfWork.JobApplications.Update(candidate.Application);
                    scored++;
                }
                else
                {
                    _logger.LogWarning(
                        "AI response omitted applicant {ApplicantKey} for job {JobId}; keeping previous score",
                        key, job.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ScoreJobApplicantsResponse(job.Id, scored, reusedCount);
    }

    private async Task<Domain.Entities.Job> GetAndValidateJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to score applicants.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to score applicants for this job.");
        }

        if (job.Status != JobStatus.Published)
        {
            throw new BadRequestException("Applicant scoring can only be triggered for published jobs.");
        }

        return job;
    }

    private async Task<TalentProfileForMatching> BuildCandidateProfileAsync(
        Domain.Entities.JobApplication application,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(application.TalentUserId))
        {
            var talent = await _unitOfWork.Talents
                .GetByUserIdAsync(application.TalentUserId, cancellationToken);

            if (talent != null)
            {
                var workExps = await _unitOfWork.WorkExperiences
                    .GetByUserIdAsync(talent.UserId, cancellationToken);
                var educations = await _unitOfWork.Educations
                    .GetByUserIdAsync(talent.UserId, cancellationToken);

                return new TalentProfileForMatching
                {
                    TalentUserId = application.TalentUserId,
                    Name = talent.Name,
                    Tagline = talent.Tagline,
                    TechnicalSkills = talent.TechnicalSkills ?? new(),
                    SoftSkills = talent.SoftSkills ?? new(),
                    ToolsAndPlatforms = talent.ToolsAndPlatforms ?? new(),
                    TotalYearsExperience = talent.TotalYearsExperience,
                    ExpectedSalary = talent.ExpectedSalary,
                    Location = talent.Location,
                    SkillPropositions = talent.SkillPropositions ?? new(),
                    WorkExperiences = workExps.Select(w => new WorkExperienceSummary
                    {
                        CompanyName = w.CompanyName,
                        JobTitle = w.JobTitle,
                        StartDate = w.StartDate,
                        EndDate = w.EndDate,
                        IsCurrent = w.IsCurrent
                    }).ToList(),
                    Educations = educations.Select(e => new EducationSummary
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate
                    }).ToList()
                };
            }
        }

        return BuildMinimalProfile(application);
    }

    private static TalentProfileForMatching BuildMinimalProfile(Domain.Entities.JobApplication application)
    {
        return new TalentProfileForMatching
        {
            TalentUserId = application.TalentUserId ?? $"guest-{application.Id}",
            Name = application.ApplicantName,
            TotalYearsExperience = application.TotalYearsExperience,
            ExpectedSalary = application.ExpectedSalary,
            Location = application.Location
        };
    }

    private static JobProfileForMatching BuildJobProfile(Domain.Entities.Job job)
    {
        return new JobProfileForMatching
        {
            JobId = job.Id,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            RequiredSkills = job.RequiredSkills ?? new(),
            ExperienceLevel = job.ExperienceLevel.ToString(),
            Location = job.Location,
            WorkArrangement = job.WorkArrangement.ToString(),
            City = job.City,
            Country = job.Country
        };
    }

    private static Dictionary<string, CandidateMatchResult> BuildMatchMap(
        List<CandidateMatchResult> matches)
    {
        return matches
            .Where(m => !string.IsNullOrWhiteSpace(m.TalentUserId))
            .GroupBy(m => m.TalentUserId!.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    private sealed record CandidateInput(
        Domain.Entities.JobApplication Application,
        TalentProfileForMatching Profile,
        string Hash);
}

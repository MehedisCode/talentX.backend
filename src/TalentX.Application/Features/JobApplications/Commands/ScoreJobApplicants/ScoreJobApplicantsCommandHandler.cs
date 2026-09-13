using MediatR;
using TalentX.Application.Common.Exceptions;
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

    public ScoreJobApplicantsCommandHandler(
        IUnitOfWork unitOfWork,
        ITalentMatchingService matchingService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _currentUserService = currentUserService;
    }

    public async Task<ScoreJobApplicantsResponse> Handle(
        ScoreJobApplicantsCommand request,
        CancellationToken cancellationToken)
    {
        var job = await GetAndValidateJobAsync(request.JobId, cancellationToken);

        var applications = await _unitOfWork.JobApplications
            .GetByJobIdAsync(job.Id, cancellationToken);

        if (applications.Count == 0)
        {
            return new ScoreJobApplicantsResponse(job.Id, 0);
        }

        var matchingRequest = await BuildMatchingRequestAsync(job, applications, cancellationToken);

        var matchResult = await _matchingService.MatchAsync(matchingRequest, cancellationToken);

        var scoredCount = await PersistScoresAsync(applications, matchResult.Matches, cancellationToken);

        return new ScoreJobApplicantsResponse(job.Id, scoredCount);
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

    private async Task<TalentMatchingRequest> BuildMatchingRequestAsync(
        Domain.Entities.Job job,
        IReadOnlyList<Domain.Entities.JobApplication> applications,
        CancellationToken cancellationToken)
    {
        var candidateProfiles = new List<TalentProfileForMatching>();

        foreach (var application in applications)
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

                    candidateProfiles.Add(new TalentProfileForMatching
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
                    });
                    continue;
                }
            }

            candidateProfiles.Add(BuildMinimalProfile(application));
        }

        return new TalentMatchingRequest
        {
            Job = new JobProfileForMatching
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
            },
            Candidates = candidateProfiles
        };
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

    private async Task<int> PersistScoresAsync(
        IReadOnlyList<Domain.Entities.JobApplication> applications,
        List<CandidateMatchResult> matches,
        CancellationToken cancellationToken)
    {
        var matchMap = matches.ToDictionary(m => m.TalentUserId);
        var scored = 0;

        foreach (var application in applications)
        {
            var key = application.TalentUserId ?? $"guest-{application.Id}";
            if (matchMap.TryGetValue(key, out var match))
            {
                application.SetAiMatchScore(match.Score);
                _unitOfWork.JobApplications.Update(application);
                scored++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return scored;
    }
}

using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Helpers;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;

public sealed class RunJobTalentMatchingCommandHandler
    : IRequestHandler<RunJobTalentMatchingCommand, RunJobTalentMatchingResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITalentMatchingService _matchingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RunJobTalentMatchingCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public RunJobTalentMatchingCommandHandler(
        IUnitOfWork unitOfWork,
        ITalentMatchingService matchingService,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        ILogger<RunJobTalentMatchingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RunJobTalentMatchingResponse> Handle(
        RunJobTalentMatchingCommand request,
        CancellationToken cancellationToken)
    {
        var batchSize = MatchingBatch.ResolveSize(_configuration);
        var job = await GetAndValidateJobAsync(request.JobId, cancellationToken);

        var eligibleTalents = await GetEligibleTalentsAsync(job, cancellationToken);
        if (eligibleTalents.Count == 0)
        {
            return new RunJobTalentMatchingResponse(job.Id, 0, new List<MatchedCandidateDto>());
        }

        var jobProfile = BuildJobProfile(job);

        var candidates = new List<CandidateInput>();
        foreach (var talent in eligibleTalents)
        {
            var profile = await BuildCandidateProfileAsync(talent, cancellationToken);
            candidates.Add(new CandidateInput(
                talent,
                profile,
                MatchingInputHash.Compute(jobProfile, profile)));
        }

        var existingMatches = await _unitOfWork.JobTalentMatches
            .GetByJobIdAsync(job.Id, cancellationToken);
        var rows = existingMatches
            .GroupBy(m => m.TalentUserId.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var toScore = candidates
            .Where(c => !rows.TryGetValue(c.Talent.UserId, out var row)
                || !string.Equals(row.InputHash, c.Hash, StringComparison.Ordinal))
            .ToList();
        var reusedCount = candidates.Count - toScore.Count;

        if (toScore.Count > 0)
        {
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
                        "AI matching failed for job {JobId} after {ScoredCount} of {PendingCount} candidates",
                        job.Id, scored, toScore.Count);
                    throw new BadGatewayException("AI service is temporarily unavailable.");
                }

                var matchMap = BuildMatchMap(matchResult.Matches);

                foreach (var candidate in batch)
                {
                    if (matchMap.TryGetValue(candidate.Talent.UserId, out var match))
                    {
                        if (rows.TryGetValue(candidate.Talent.UserId, out var existing))
                        {
                            existing.UpdateScore(match.Score, match.Explanation, candidate.Hash);
                            _unitOfWork.JobTalentMatches.Update(existing);
                        }
                        else
                        {
                            var created = new JobTalentMatch(
                                job.Id, candidate.Talent.UserId, match.Score, match.Explanation, candidate.Hash);
                            await _unitOfWork.JobTalentMatches.AddAsync(created, cancellationToken);
                            rows[candidate.Talent.UserId] = created;
                        }

                        scored++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "AI response omitted candidate {TalentUserId} for job {JobId}; keeping previous match",
                            candidate.Talent.UserId, job.Id);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation(
                "All {Count} candidates for job {JobId} reused existing AI match scores",
                reusedCount, job.Id);
        }

        var eligibleUserIds = eligibleTalents
            .Select(t => t.UserId)
            .ToHashSet(StringComparer.Ordinal);

        var rankedCandidates = await BuildMatchedCandidateDtosAsync(
            job.Id,
            eligibleTalents,
            rows.Values.Where(m => eligibleUserIds.Contains(m.TalentUserId)),
            cancellationToken);

        return new RunJobTalentMatchingResponse(
            job.Id, rankedCandidates.Count, rankedCandidates, reusedCount);
    }

    private async Task<Job> GetAndValidateJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to run AI matching.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to run AI matching for this job.");
        }

        var effectiveStatus = job.GetEffectiveStatus();
        if (effectiveStatus is not (JobStatus.Published or JobStatus.Expired))
        {
            throw new BadRequestException("AI matching can only be triggered for active or expired jobs.");
        }

        return job;
    }

    private async Task<List<Talent>> GetEligibleTalentsAsync(Job job, CancellationToken cancellationToken)
    {
        var allTalents = await _unitOfWork.Talents.GetAllAsync(cancellationToken);

        var existingApplications = _unitOfWork.JobApplications != null
            ? await _unitOfWork.JobApplications.GetByJobIdAsync(job.Id, cancellationToken)
            : Array.Empty<JobApplication>();

        var appliedUserIds = existingApplications
            .Where(a => !string.IsNullOrEmpty(a.TalentUserId))
            .Select(a => a.TalentUserId!)
            .ToHashSet();

        var eligibleTalents = allTalents
            .Where(t => !string.IsNullOrWhiteSpace(t.Name) &&
                        !appliedUserIds.Contains(t.UserId) &&
                        (t.TechnicalSkills.Count > 0 ||
                         t.TotalYearsExperience.HasValue ||
                         !string.IsNullOrWhiteSpace(t.Tagline) ||
                         t.ToolsAndPlatforms.Count > 0))
            .ToList();

        if (eligibleTalents.Count > 50)
        {
            var requiredSkillsSet = job.RequiredSkills.Select(s => s.ToLowerInvariant()).ToHashSet();
            eligibleTalents = eligibleTalents
                .OrderByDescending(t => t.TechnicalSkills.Count(s => requiredSkillsSet.Contains(s.ToLowerInvariant())))
                .ThenByDescending(t => t.TotalYearsExperience ?? 0)
                .ThenBy(t => t.UserId)
                .Take(50)
                .ToList();
        }

        return eligibleTalents;
    }

    private async Task<TalentProfileForMatching> BuildCandidateProfileAsync(
        Talent talent,
        CancellationToken cancellationToken)
    {
        var workExps = await _unitOfWork.WorkExperiences.GetByUserIdAsync(talent.UserId, cancellationToken);
        var educations = await _unitOfWork.Educations.GetByUserIdAsync(talent.UserId, cancellationToken);

        return new TalentProfileForMatching
        {
            TalentUserId = talent.UserId,
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

    private static JobProfileForMatching BuildJobProfile(Job job)
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

    private async Task<List<MatchedCandidateDto>> BuildMatchedCandidateDtosAsync(
        Guid jobId,
        List<Talent> eligibleTalents,
        IEnumerable<JobTalentMatch> matches,
        CancellationToken cancellationToken)
    {
        var invitations = await _unitOfWork.Invitations.GetByJobIdAsync(jobId, cancellationToken);
        var invitedUserIds = invitations
            .Where(i => i.IsActive)
            .Select(i => i.TalentUserId)
            .ToHashSet();

        var talentMap = eligibleTalents.ToDictionary(t => t.UserId, StringComparer.Ordinal);

        return matches
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.TalentUserId, StringComparer.Ordinal)
            .Select(m =>
            {
                talentMap.TryGetValue(m.TalentUserId, out var talent);
                return new MatchedCandidateDto
                {
                    CandidateId = talent?.Id ?? Guid.Empty,
                    TalentUserId = m.TalentUserId,
                    Name = talent?.Name ?? "Candidate",
                    Tagline = talent?.Tagline,
                    Location = talent?.Location,
                    TotalYearsExperience = talent?.TotalYearsExperience,
                    ExpectedSalary = talent?.ExpectedSalary,
                    TechnicalSkills = talent?.TechnicalSkills ?? new(),
                    Score = m.Score,
                    Explanation = m.CriteriaSnapshot,
                    HasActiveInvitation = invitedUserIds.Contains(m.TalentUserId),
                    MatchedAt = m.MatchedAt
                };
            })
            .ToList();
    }

    private sealed record CandidateInput(
        Talent Talent,
        TalentProfileForMatching Profile,
        string Hash);
}

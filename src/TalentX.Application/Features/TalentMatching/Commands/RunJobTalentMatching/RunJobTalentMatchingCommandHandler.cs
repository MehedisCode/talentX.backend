using MediatR;
using TalentX.Application.Common.Exceptions;
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

    public RunJobTalentMatchingCommandHandler(
        IUnitOfWork unitOfWork,
        ITalentMatchingService matchingService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _currentUserService = currentUserService;
    }

    public async Task<RunJobTalentMatchingResponse> Handle(
        RunJobTalentMatchingCommand request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to run AI matching.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to run AI matching for this job.");
        }

        if (job.Status != JobStatus.Published)
        {
            throw new BadRequestException("AI matching can only be triggered for published jobs.");
        }

        // 1. Identify eligible talent profiles
        var allTalents = await _unitOfWork.Talents.GetAllAsync(cancellationToken);

        // Filter out candidates who have already applied to this job
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

        if (eligibleTalents.Count == 0)
        {
            return new RunJobTalentMatchingResponse(job.Id, 0, new List<MatchedCandidateDto>());
        }

        // Limit candidate batch size to top 50 candidates to prevent AI token window overflow and timeouts
        if (eligibleTalents.Count > 50)
        {
            var requiredSkillsSet = job.RequiredSkills.Select(s => s.ToLowerInvariant()).ToHashSet();
            eligibleTalents = eligibleTalents
                .OrderByDescending(t => t.TechnicalSkills.Count(s => requiredSkillsSet.Contains(s.ToLowerInvariant())))
                .ThenByDescending(t => t.TotalYearsExperience ?? 0)
                .Take(50)
                .ToList();
        }

        // 2. Build structured candidate data for AI matching
        var candidateProfiles = new List<TalentProfileForMatching>();
        foreach (var talent in eligibleTalents)
        {
            var workExps = await _unitOfWork.WorkExperiences.GetByUserIdAsync(talent.UserId, cancellationToken);
            var educations = await _unitOfWork.Educations.GetByUserIdAsync(talent.UserId, cancellationToken);

            candidateProfiles.Add(new TalentProfileForMatching
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
            });
        }

        var matchingRequest = new TalentMatchingRequest
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

        // 3. Call AI matching service (if it throws, nothing is persisted)
        var matchResult = await _matchingService.MatchAsync(matchingRequest, cancellationToken);

        // 4. Persist matches in JobTalentMatches table
        var existingMatches = await _unitOfWork.JobTalentMatches.GetByJobIdAsync(job.Id, cancellationToken);
        var existingMap = existingMatches.ToDictionary(m => m.TalentUserId);

        foreach (var item in matchResult.Matches)
        {
            if (existingMap.TryGetValue(item.TalentUserId, out var existing))
            {
                existing.UpdateScore(item.Score, item.Explanation);
                _unitOfWork.JobTalentMatches.Update(existing);
            }
            else
            {
                var newMatch = new JobTalentMatch(job.Id, item.TalentUserId, item.Score, item.Explanation);
                await _unitOfWork.JobTalentMatches.AddAsync(newMatch, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Query active invitations for this job
        var invitations = await _unitOfWork.Invitations.GetByJobIdAsync(job.Id, cancellationToken);
        var invitedUserIds = invitations
            .Where(i => i.IsActive)
            .Select(i => i.TalentUserId)
            .ToHashSet();

        var talentMap = eligibleTalents.ToDictionary(t => t.UserId);

        var rankedCandidates = matchResult.Matches
            .OrderByDescending(m => m.Score)
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
                    Explanation = m.Explanation,
                    HasActiveInvitation = invitedUserIds.Contains(m.TalentUserId),
                    MatchedAt = DateTime.UtcNow
                };
            })
            .ToList();

        return new RunJobTalentMatchingResponse(job.Id, rankedCandidates.Count, rankedCandidates);
    }
}

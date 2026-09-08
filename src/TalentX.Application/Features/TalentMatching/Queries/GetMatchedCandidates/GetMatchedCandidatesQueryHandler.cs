using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;

public sealed class GetMatchedCandidatesQueryHandler
    : IRequestHandler<GetMatchedCandidatesQuery, List<MatchedCandidateDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMatchedCandidatesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<MatchedCandidateDto>> Handle(
        GetMatchedCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var job = await GetAndValidateJobAccessAsync(request.JobId, cancellationToken);

        var matches = await _unitOfWork.JobTalentMatches.GetByJobIdAsync(job.Id, cancellationToken);
        if (matches.Count == 0)
        {
            return new List<MatchedCandidateDto>();
        }

        return await MapToMatchedCandidateDtosAsync(job.Id, matches, cancellationToken);
    }

    private async Task<Job> GetAndValidateJobAccessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to view candidate matches.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to view candidates for this job.");
        }

        return job;
    }

    private async Task<List<MatchedCandidateDto>> MapToMatchedCandidateDtosAsync(
        Guid jobId,
        IReadOnlyList<JobTalentMatch> matches,
        CancellationToken cancellationToken)
    {
        var applications = _unitOfWork.JobApplications != null
            ? await _unitOfWork.JobApplications.GetByJobIdAsync(jobId, cancellationToken)
            : Array.Empty<JobApplication>();

        var appliedUserIds = applications
            .Where(a => !string.IsNullOrEmpty(a.TalentUserId))
            .Select(a => a.TalentUserId!)
            .ToHashSet();

        var invitations = await _unitOfWork.Invitations.GetByJobIdAsync(jobId, cancellationToken);
        var invitedUserIds = invitations
            .Where(i => i.IsActive)
            .Select(i => i.TalentUserId)
            .ToHashSet();

        var allTalents = await _unitOfWork.Talents.GetAllAsync(cancellationToken);
        var talentMap = allTalents.ToDictionary(t => t.UserId);

        return matches
            .Where(m => !appliedUserIds.Contains(m.TalentUserId))
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
                    Explanation = m.CriteriaSnapshot,
                    HasActiveInvitation = invitedUserIds.Contains(m.TalentUserId),
                    MatchedAt = m.MatchedAt
                };
            })
            .ToList();
    }
}

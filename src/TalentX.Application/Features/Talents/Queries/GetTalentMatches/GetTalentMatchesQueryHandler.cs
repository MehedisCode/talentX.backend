using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentMatches;

public sealed class GetTalentMatchesQueryHandler
    : IRequestHandler<GetTalentMatchesQuery, PaginatedList<TalentMatchDto>>
{

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTalentMatchesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<TalentMatchDto>> Handle(
        GetTalentMatchesQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to access your matches.");

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(
            currentUserId,
            cancellationToken);

        if(talent is null)
        {
            throw new NotFoundException("Talent not found.");
        }

        if (talent.TechnicalSkills.Count == 0)
        {
            return new PaginatedList<TalentMatchDto>([], page, pageSize, 0);
        }

        var talentTechnicalSkillSet = talent.TechnicalSkills
            .Select(s => s.Trim().ToLowerInvariant())
            .ToHashSet();
        
        var talentSoftSkillSet = talent.SoftSkills
            .Select(s => s.Trim().ToLowerInvariant())
            .ToHashSet();
        
        var talentToolsAndPlatforms = talent.ToolsAndPlatforms
            .Select(s => s.Trim().ToLowerInvariant())
            .ToHashSet();

        var openJobs = await _unitOfWork.Jobs.GetActivePublishedJobsAsync(cancellationToken);

        decimal MatchThreshold = request.MinScore;

        var matches = new List<TalentMatchDto>();

        foreach (var job in openJobs)
        {
            bool hasApplied = await _unitOfWork.JobApplications
                .ExistsByJobIdAndTalentUserIdAsync(job.Id, talent.UserId, cancellationToken);

            if (job.RequiredSkills.Count == 0 || hasApplied)
            {
                continue; // can't meaningfully score a job with no required skills
            }

            var requiredSkillSet = job.RequiredSkills
                .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();


            var matchedCount = requiredSkillSet.Count(skill => talentTechnicalSkillSet.Contains(skill));
            matchedCount += requiredSkillSet.Count(skill => talentSoftSkillSet.Contains(skill));
            matchedCount += requiredSkillSet.Count(skill => talentToolsAndPlatforms.Contains(skill));

            var matchRatio = (decimal)matchedCount / requiredSkillSet.Count;
            var score = (int)Math.Round(matchRatio * 100);

            if (score >= MatchThreshold)
            {
                matches.Add(new TalentMatchDto(
                    job.Id,
                    job.Title,
                    job.CompanyName,
                    score,
                    job.WorkArrangement,
                    job.EmploymentType,
                    job.ExpireAt
                ));
            }
        }

        // 4. Sort by best match first, then paginate in-memory
        var ordered = matches
            .OrderByDescending(m => m.MatchScore)
            .ToList();

        var totalCount = ordered.Count;
        var pagedItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<TalentMatchDto>(pagedItems, page, pageSize, totalCount);
    }
}
using MediatR;
using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Domain;

namespace TalentX.Application.Features.TalentMatching.Queries.GetCandidateSummary;

public sealed class GetCandidateSummaryQueryHandler
    : IRequestHandler<GetCandidateSummaryQuery, CandidateProfileSummary>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly IConfiguration _configuration;

    public GetCandidateSummaryQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
        _configuration = configuration;
    }

    public async Task<CandidateProfileSummary> Handle(
        GetCandidateSummaryQuery request,
        CancellationToken cancellationToken)
    {
        Talent? talent = null;

        if (Guid.TryParse(request.Identifier, out var guidId))
        {
            talent = await _unitOfWork.Talents.GetByIdAsync(guidId, cancellationToken);
        }

        talent ??= await _unitOfWork.Talents.GetByUserIdAsync(request.Identifier, cancellationToken);

        if (talent is null)
        {
            throw new NotFoundException($"Candidate '{request.Identifier}' was not found.");
        }

        var workExps = await _unitOfWork.WorkExperiences.GetByUserIdAsync(talent.UserId, cancellationToken);
        var educations = await _unitOfWork.Educations.GetByUserIdAsync(talent.UserId, cancellationToken);

        string? profilePictureUrl = null;
        if (!string.IsNullOrWhiteSpace(talent.ProfilePictureStorageKey))
        {
            var expiryStr = _configuration["PRESIGNED_URL_EXPIRY_MINUTES"]
                ?? _configuration["PresignedUrl:ExpiryMinutes"];

            var expiryMinutes = int.TryParse(expiryStr, out var parsedMinutes) && parsedMinutes > 0
                ? parsedMinutes
                : 15;

            var downloadUrlExpiry = TimeSpan.FromMinutes(expiryMinutes);

            profilePictureUrl = await _presignedUrlService.CreateDownloadUrlAsync(
                talent.ProfilePictureStorageKey, downloadUrlExpiry, cancellationToken);
        }

        return new CandidateProfileSummary
        {
            Id = talent.Id,
            UserId = talent.UserId,
            Name = talent.Name,
            Email = talent.Email,
            Tagline = talent.Tagline,
            ShortBio = talent.ShortBio,
            Location = talent.Location,
            Phone = talent.Phone,
            LinkedInUrl = talent.LinkedInUrl,
            AdditionalUrls = talent.AdditionalUrls ?? new(),
            TotalYearsExperience = talent.TotalYearsExperience,
            ExpectedSalary = talent.ExpectedSalary,
            SkillPropositions = talent.SkillPropositions ?? new(),
            TechnicalSkills = talent.TechnicalSkills ?? new(),
            SoftSkills = talent.SoftSkills ?? new(),
            ToolsAndPlatforms = talent.ToolsAndPlatforms ?? new(),
            ProfilePictureUrl = profilePictureUrl,
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

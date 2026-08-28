using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfile;

public sealed class GetTalentProfileQueryHandler : IRequestHandler<GetTalentProfileQuery, TalentProfileResponse>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTalentProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<TalentProfileResponse> Handle(GetTalentProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your profile.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        var workExperiencesTask = _unitOfWork.WorkExperiences.GetByUserIdAsync(userId, cancellationToken);
        var educationsTask = _unitOfWork.Educations.GetByUserIdAsync(userId, cancellationToken);
        var resumeTask = _unitOfWork.Resumes.GetByUserIdAsync(userId, cancellationToken);

        await Task.WhenAll(workExperiencesTask, educationsTask, resumeTask);

        var workExperiences = await workExperiencesTask;
        var educations = await educationsTask;
        var resume = await resumeTask;

        var profilePictureUrlTask = CreateDownloadUrlOrNullAsync(talent.ProfilePictureStorageKey, cancellationToken);
        var videoIntroUrlTask = CreateDownloadUrlOrNullAsync(talent.VideoIntroStorageKey, cancellationToken);
        var resumeUrlTask = CreateDownloadUrlOrNullAsync(resume?.StorageKey, cancellationToken);

        await Task.WhenAll(profilePictureUrlTask, videoIntroUrlTask, resumeUrlTask);

        var profilePictureUrl = await profilePictureUrlTask;
        var videoIntroUrl = await videoIntroUrlTask;
        var resumeUrl = await resumeUrlTask;

        return new TalentProfileResponse(
            talent.Id,
            talent.UserId,
            talent.Name,
            talent.Email,
            talent.Tagline,
            talent.ShortBio,
            talent.Location,
            talent.Phone,
            talent.LinkedInUrl,
            talent.AdditionalUrls,
            talent.TotalYearsExperience,
            talent.ExpectedSalary,
            talent.SkillPropositions,
            talent.TechnicalSkills,
            talent.SoftSkills,
            talent.ToolsAndPlatforms,
            profilePictureUrl,
            videoIntroUrl,
            resume?.OriginalFileName,
            resumeUrl,
            _mapper.Map<List<WorkExperienceItemDto>>(workExperiences),
            _mapper.Map<List<EducationItemDto>>(educations),
            talent.CreatedAt,
            talent.UpdatedAt
        );
    }

    private async Task<string?> CreateDownloadUrlOrNullAsync(string? storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        return await _presignedUrlService.CreateDownloadUrlAsync(storageKey, DownloadUrlExpiry, cancellationToken);
    }
}

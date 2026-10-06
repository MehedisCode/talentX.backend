using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Helpers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;

public sealed class GetTalentProfileForEmployerQueryHandler
    : IRequestHandler<GetTalentProfileForEmployerQuery, TalentPublicProfileResponse>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(60);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTalentProfileForEmployerQueryHandler(
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

    public async Task<TalentPublicProfileResponse> Handle(
        GetTalentProfileForEmployerQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to view candidate profile.");

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException($"Job application '{request.ApplicationId}' was not found.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(application.JobId, cancellationToken);
        if (job is null || job.EmployerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view candidate profile for this job.");
        }

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(application.TalentUserId, cancellationToken);
        var workExperiences = await _unitOfWork.WorkExperiences.GetByUserIdAsync(application.TalentUserId, cancellationToken);
        var educations = await _unitOfWork.Educations.GetByUserIdAsync(application.TalentUserId, cancellationToken);
        var resume = await _unitOfWork.Resumes.GetByUserIdAsync(application.TalentUserId, cancellationToken);

        // Resume URL prioritizing application resume, falling back to profile resume
        string? resumeStorageKey = !string.IsNullOrWhiteSpace(application.ResumeStorageKey)
            ? application.ResumeStorageKey
            : resume?.StorageKey;

        var profilePictureUrlTask = CreateDownloadUrlOrNullAsync(talent?.ProfilePictureStorageKey, cancellationToken);
        var videoIntroUrlTask = CreateDownloadUrlOrNullAsync(talent?.VideoIntroStorageKey, cancellationToken);
        var resumeUrlTask = CreateDownloadUrlOrNullAsync(resumeStorageKey, cancellationToken);

        await Task.WhenAll(profilePictureUrlTask, videoIntroUrlTask, resumeUrlTask);

        string? resumeOriginalFileName = resume?.OriginalFileName;
        if (string.IsNullOrWhiteSpace(resumeOriginalFileName) && !string.IsNullOrWhiteSpace(application.ResumeStorageKey))
        {
            var fileNameSegment = Path.GetFileName(application.ResumeStorageKey);
            var firstUnderscore = fileNameSegment.IndexOf('_');
            if (firstUnderscore > 0 && firstUnderscore < fileNameSegment.Length - 1 && Guid.TryParse(fileNameSegment[..firstUnderscore], out _))
            {
                resumeOriginalFileName = fileNameSegment[(firstUnderscore + 1)..];
            }
            else
            {
                resumeOriginalFileName = fileNameSegment;
            }
        }

        var mappedWorkExperiences = _mapper.Map<List<WorkExperienceItemDto>>(workExperiences);
        var sortedWorkExperiences = ChronologicalOrderingHelper.SortWorkExperiences(
            mappedWorkExperiences,
            experience => (experience.IsCurrent, experience.StartDate, experience.EndDate));

        var mappedEducations = _mapper.Map<List<EducationItemDto>>(educations);
        var sortedEducations = ChronologicalOrderingHelper.SortEducations(
            mappedEducations,
            education => (education.StartDate, education.EndDate));

        return new TalentPublicProfileResponse(
            ApplicationId: application.Id,
            JobId: application.JobId,
            TalentUserId: application.TalentUserId,
            Name: talent?.Name ?? application.ApplicantName,
            Email: talent?.Email ?? application.ApplicantEmail,
            Tagline: talent?.Tagline,
            ShortBio: talent?.ShortBio,
            Location: talent?.Location ?? application.Location,
            LinkedInUrl: talent?.LinkedInUrl,
            AdditionalUrls: talent?.AdditionalUrls ?? new List<string>(),
            TotalYearsExperience: talent?.TotalYearsExperience ?? application.TotalYearsExperience,
            ExpectedSalary: talent?.ExpectedSalary ?? application.ExpectedSalary,
            SkillPropositions: talent?.SkillPropositions ?? new List<string>(),
            TechnicalSkills: talent?.TechnicalSkills ?? new List<string>(),
            SoftSkills: talent?.SoftSkills ?? new List<string>(),
            ToolsAndPlatforms: talent?.ToolsAndPlatforms ?? new List<string>(),
            ProfilePictureUrl: await profilePictureUrlTask,
            VideoIntroUrl: await videoIntroUrlTask,
            ResumeOriginalFileName: resumeOriginalFileName ?? "Resume.pdf",
            ResumeUrl: await resumeUrlTask,
            ApplicationStatus: application.Status.ToString(),
            AiMatchScore: application.AiMatchScore,
            CoverLetter: application.CoverLetter,
            AppliedAt: application.AppliedAt,
            WorkExperiences: sortedWorkExperiences,
            Educations: sortedEducations
        );
    }

    private async Task<string?> CreateDownloadUrlOrNullAsync(string? storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        return await _presignedUrlService.CreateDownloadUrlAsync(storageKey, DownloadUrlExpiry, cancellationToken);
    }
}

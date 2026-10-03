using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;

public sealed record TalentPublicProfileResponse(
    Guid ApplicationId,
    Guid JobId,
    string TalentUserId,
    string Name,
    string Email,
    string? Tagline,
    string? ShortBio,
    string? Location,
    string? LinkedInUrl,
    List<string> AdditionalUrls,
    decimal? TotalYearsExperience,
    decimal? ExpectedSalary,
    List<string> SkillPropositions,
    List<string> TechnicalSkills,
    List<string> SoftSkills,
    List<string> ToolsAndPlatforms,
    string? ProfilePictureUrl,
    string? VideoIntroUrl,
    string? ResumeOriginalFileName,
    string? ResumeUrl,
    string ApplicationStatus,
    decimal? AiMatchScore,
    string? CoverLetter,
    DateTime AppliedAt,
    IReadOnlyList<WorkExperienceItemDto> WorkExperiences,
    IReadOnlyList<EducationItemDto> Educations
);

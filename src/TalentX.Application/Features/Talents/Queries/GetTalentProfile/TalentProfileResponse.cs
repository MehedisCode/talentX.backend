using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfile;

public sealed record TalentProfileResponse(
    Guid Id,
    string UserId,
    string Name,
    string Email,
    string? Tagline,
    string? ShortBio,
    string? Location,
    string? Phone,
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
    IReadOnlyList<WorkExperienceItemDto> WorkExperiences,
    IReadOnlyList<EducationItemDto> Educations,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

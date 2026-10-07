namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedProfileData(
    string? FullName,
    string? Tagline,
    string? ShortBio,
    string? Location,
    string? Phone,
    string? LinkedInUrl,
    List<string>? AdditionalUrls,
    decimal? TotalYearsExperience,
    decimal? ExpectedSalary,
    List<string>? SkillPropositions,
    List<string>? TechnicalSkills,
    List<string>? SoftSkills,
    List<string>? ToolsAndPlatforms,
    List<ApplyExtractedWorkExperienceItem>? WorkExperiences,
    List<ApplyExtractedEducationItem>? Educations
);

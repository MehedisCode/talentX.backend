using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

namespace TalentX.API.Controllers.Talents;

/// <summary>
/// Shape of the "payload" JSON string form field on the multipart
/// <c>POST /me/profile/apply-extraction</c> request — everything from
/// <see cref="ApplyExtractedProfileCommand"/> except the resume file itself,
/// which travels as its own form part.
/// </summary>
public sealed record ApplyExtractedProfilePayload(
    List<string> TechnicalSkills,
    List<string> SoftSkills,
    List<string> ToolsAndPlatforms,
    List<ApplyExtractedWorkExperienceItem> WorkExperiences,
    List<ApplyExtractedEducationItem> Educations
);

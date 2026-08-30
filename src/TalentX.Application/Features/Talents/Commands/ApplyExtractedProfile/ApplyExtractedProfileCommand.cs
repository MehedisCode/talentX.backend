using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedProfileCommand(
    List<string> TechnicalSkills,
    List<string> SoftSkills,
    List<string> ToolsAndPlatforms,
    List<ApplyExtractedWorkExperienceItem> WorkExperiences,
    List<ApplyExtractedEducationItem> Educations,
    IUploadedFile ResumeFile
) : IRequest;

using MediatR;

namespace TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;

public sealed record UpdateTalentProfileCommand(
    string Name,
    string? Location = null,
    string? Tagline = null,
    string? ShortBio = null,
    string? Phone = null,
    string? LinkedInUrl = null,
    List<string>? AdditionalUrls = null,
    decimal? TotalYearsExperience = null,
    decimal? ExpectedSalary = null,
    List<string>? SkillPropositions = null,
    List<string>? TechnicalSkills = null,
    List<string>? SoftSkills = null,
    List<string>? ToolsAndPlatforms = null
) : IRequest;

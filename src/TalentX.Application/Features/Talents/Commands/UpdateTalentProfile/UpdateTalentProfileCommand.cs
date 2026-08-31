using MediatR;

namespace TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;

public sealed record UpdateTalentProfileCommand(
    string Name,
    string? Location,
    string? Tagline,
    string? ShortBio,
    string? Phone,
    string? LinkedInUrl,
    List<string>? AdditionalUrls,
    decimal? TotalYearsExperience,
    decimal? ExpectedSalary,
    List<string>? SkillPropositions
) : IRequest;

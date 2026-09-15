using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.DTOs;

public sealed record AdminUserDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsVerified { get; init; }
    public AccountStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public AdminTalentDetailsSectionDto? TalentDetails { get; init; }
    public AdminEmployerDetailsSectionDto? EmployerDetails { get; init; }
}

public sealed record AdminTalentDetailsSectionDto
{
    public string? Tagline { get; init; }
    public string? ShortBio { get; init; }
    public string? Location { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public List<string> AdditionalUrls { get; init; } = new();
    public decimal? TotalYearsExperience { get; init; }
    public decimal? ExpectedSalary { get; init; }
    public List<string> SkillPropositions { get; init; } = new();
    public List<string> TechnicalSkills { get; init; } = new();
    public List<string> SoftSkills { get; init; } = new();
    public List<string> ToolsAndPlatforms { get; init; } = new();
}

public sealed record AdminEmployerDetailsSectionDto
{
    public string CompanyName { get; init; } = string.Empty;
}

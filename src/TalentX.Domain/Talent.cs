using TalentX.Domain.Enums;

namespace TalentX.Domain;

public sealed class Talent : User
{
    public string? ProfilePictureStorageKey { get; private set; }
    public string? Tagline { get; private set; }
    public string? ShortBio { get; private set; }
    public string? Location { get; private set; }
    public string? Phone { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public List<string> AdditionalUrls { get; private set; } = new();
    public string? VideoIntroStorageKey { get; private set; }
    public decimal? TotalYearsExperience { get; private set; }
    public decimal? ExpectedSalary { get; private set; }
    public List<string> SkillPropositions { get; private set; } = new();
    public List<string> TechnicalSkills { get; private set; } = new();
    public List<string> SoftSkills { get; private set; } = new();
    public List<string> ToolsAndPlatforms { get; private set; } = new();

    private Talent() : base() { }

    public Talent(string userId, string name, string email)
        : base(userId, name, email, UserRole.Talent)
    {
    }

    public void UpdateProfile(
        string name,
        string? tagline,
        string? shortBio,
        string? location,
        string? phone,
        string? linkedInUrl,
        List<string>? additionalUrls,
        decimal? totalYearsExperience,
        decimal? expectedSalary,
        List<string>? skillPropositions)
    {
        UpdateName(name);
        Tagline = tagline;
        ShortBio = shortBio;
        Location = location;
        Phone = phone;
        LinkedInUrl = linkedInUrl;
        AdditionalUrls = additionalUrls ?? new List<string>();
        TotalYearsExperience = totalYearsExperience;
        ExpectedSalary = expectedSalary;
        SkillPropositions = skillPropositions ?? new List<string>();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfilePicture(string? profilePictureStorageKey)
    {
        ProfilePictureStorageKey = profilePictureStorageKey;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateVideoIntro(string? videoIntroStorageKey)
    {
        VideoIntroStorageKey = videoIntroStorageKey;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSkills(
        List<string>? technicalSkills,
        List<string>? softSkills,
        List<string>? toolsAndPlatforms)
    {
        TechnicalSkills = technicalSkills ?? new List<string>();
        SoftSkills = softSkills ?? new List<string>();
        ToolsAndPlatforms = toolsAndPlatforms ?? new List<string>();
        UpdatedAt = DateTime.UtcNow;
    }
}

using TalentX.Domain.Common;

namespace TalentX.Domain;

public class Company : BaseEntity
{
    public string EmployerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LogoStorageKey { get; private set; }
    public string? Description { get; private set; }
    public string? Industry { get; private set; }
    public string? CompanySize { get; private set; }
    public string? Website { get; private set; }
    public string? Location { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? TwitterUrl { get; private set; }
    public string? FacebookUrl { get; private set; }

    private Company() : base() { }

    public Company(string employerId, string name) : base()
    {
        EmployerId = employerId;
        Name = name;
    }

    public void UpdateProfile(
        string name,
        string? description,
        string? industry,
        string? companySize,
        string? website,
        string? location,
        string? linkedInUrl,
        string? twitterUrl,
        string? facebookUrl)
    {
        Name = name;
        Description = description;
        Industry = industry;
        CompanySize = companySize;
        Website = website;
        Location = location;
        LinkedInUrl = linkedInUrl;
        TwitterUrl = twitterUrl;
        FacebookUrl = facebookUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLogo(string? logoStorageKey)
    {
        LogoStorageKey = logoStorageKey;
        UpdatedAt = DateTime.UtcNow;
    }
}

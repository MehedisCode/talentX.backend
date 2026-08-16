using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

/// <summary>
/// Represents an Employer entity in the domain, inheriting from <see cref="User"/>.
/// </summary>
public sealed class Employer : User
{
    public string CompanyName { get; private set; } = string.Empty;

    private Employer() : base() { }

    public Employer(
        string userId,
        string name,
        string email,
        string companyName
    ) : base(userId, name, email, UserRole.Employer)
    {
        CompanyName = companyName;
    }

    public void UpdateCompany(string companyName)
    {
        CompanyName = companyName;
        UpdatedAt = DateTime.UtcNow;
    }
}

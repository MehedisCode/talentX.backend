using TalentX.Domain.Enums;

namespace TalentX.Domain;

public sealed class Employer : User
{
    public string CompanyName { get; private set; } = string.Empty;

    private Employer() : base() { }

    public Employer(
        string userId,
        string name,
        string email,
        string companyName
    ) : base(userId, name, email, UserRole.Employer, status: AccountStatus.Pending)
    {
        CompanyName = companyName;
    }

    public void Update(string name, string companyName)
    {
        UpdateName(name);
        CompanyName = companyName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCompany(string companyName)
    {
        CompanyName = companyName;
        UpdatedAt = DateTime.UtcNow;
    }
}

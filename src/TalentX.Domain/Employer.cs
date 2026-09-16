using TalentX.Domain.Enums;

namespace TalentX.Domain;

public sealed class Employer : User
{
    public string CompanyName { get; private set; } = string.Empty;
    public EmployerVerificationStatus VerificationStatus { get; private set; } = EmployerVerificationStatus.PendingVerification;
    public string? RejectionReason { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

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

    public new void MarkAsVerified()
    {
        base.MarkAsVerified();
        VerificationStatus = EmployerVerificationStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        RejectionReason = null;
        RejectedAt = null;
    }

    public void MarkAsRejected(string reason)
    {
        VerificationStatus = EmployerVerificationStatus.Rejected;
        RejectionReason = reason;
        RejectedAt = DateTime.UtcNow;
        VerifiedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetVerification()
    {
        VerificationStatus = EmployerVerificationStatus.PendingVerification;
        RejectionReason = null;
        VerifiedAt = null;
        RejectedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Employers;

public sealed class CurrentEmployerResult
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public EmployerVerificationStatus VerificationStatus { get; set; } = EmployerVerificationStatus.PendingVerification;
    public string? RejectionReason { get; set; }
}

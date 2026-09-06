namespace TalentX.Application.Features.SavedJobs.DTOs;

public class SavedJobResponse
{
    public Guid SavedJobId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
    public bool IsAvailable { get; set; }
}

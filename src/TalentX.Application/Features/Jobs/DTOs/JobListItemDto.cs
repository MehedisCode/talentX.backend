namespace TalentX.Application.Features.Jobs.DTOs;

public class JobListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? RoleType { get; set; }
}

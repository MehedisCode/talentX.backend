namespace TalentX.Application.Features.Users.DTOs;

public sealed record AdminUserStatsDto
{
    public int TotalUsers { get; init; }
    public int TalentsCount { get; init; }
    public int EmployersCount { get; init; }
    public int GuestsCount { get; init; }
    public int PendingCount { get; init; }
    public int DeactivatedCount { get; init; }
}

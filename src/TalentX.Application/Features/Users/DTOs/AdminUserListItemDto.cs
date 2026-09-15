using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.DTOs;

public sealed record AdminUserListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsVerified { get; init; }
    public AccountStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

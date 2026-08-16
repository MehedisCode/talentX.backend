using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

/// <summary>
/// Represents a base user in the domain.
/// </summary>
public class User : BaseEntity
{
    public string UserId { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public UserRole Role { get; protected set; }
    public bool IsVerified { get; protected set; } = false;

    protected User() { }

    public User(
        string userId,
        string name,
        string email,
        UserRole role
    )
    {
        UserId = userId;
        Name = name;
        Email = email;
        Role = role;
        IsVerified = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name
    )
    {
        Name = name;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
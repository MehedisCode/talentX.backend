using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain;

public class User : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsVerified { get; private set; }

    protected User() { }

    public User(
        string userId,
        string name,
        string email,
        UserRole role,
        bool isVerified = false
    )
    {
        UserId = userId;
        Name = name;
        Email = email;
        Role = role;
        IsVerified = isVerified;
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(UserRole role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}

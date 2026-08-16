using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

/// <summary>
/// Represents a base user in the domain.
/// </summary>
public class User : BaseEntity
{
    public string UserId { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;

    protected User() : base() { }

    public User(
        string userId,
        string name,
        string email
    ) : base()
    {
        UserId = userId;
        Name = name;
        Email = email;
    }

    public void Update(
        string name
    )
    {
        Name = name;

        UpdatedAt = DateTime.UtcNow;
    }
}
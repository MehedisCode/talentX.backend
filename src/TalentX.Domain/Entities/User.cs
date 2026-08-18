using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public class User : BaseEntity
{
    public string UserId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }

    public User(
        string userId,
        string name,
        string email
    )
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
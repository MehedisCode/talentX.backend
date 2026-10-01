using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string Actor { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public string? IpAddress { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(string actor, string action, bool isSystem = false, string? ipAddress = null)
    {
        Actor = actor;
        Action = action;
        IsSystem = isSystem;
        IpAddress = ipAddress;
        CreatedAt = DateTime.UtcNow;
    }
}

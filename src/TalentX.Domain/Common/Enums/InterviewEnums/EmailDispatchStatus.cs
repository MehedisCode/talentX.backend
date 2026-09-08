namespace TalentX.Domain.Enums;

public enum EmailDispatchStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    MaxRetriesExceeded = 5
}

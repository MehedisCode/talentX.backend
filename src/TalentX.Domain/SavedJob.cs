using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public class SavedJob : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentId { get; private set; } = string.Empty;

    private SavedJob() { }

    public static SavedJob Create(Guid jobId, string talentId)
    {
        return new SavedJob
        {
            JobId = jobId,
            TalentId = talentId
        };
    }
}

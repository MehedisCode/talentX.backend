using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

// One row per AI generation call (initial / regenerate / additional questions).
// Daily quotas count these events instead of GenerationCount sums, which mixed
// creations and regenerations of preps created on different days.
public sealed class InterviewPreparationGeneration : BaseEntity
{
    public Guid InterviewPreparationId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public InterviewPreparationGenerationKind Kind { get; private set; }

    private InterviewPreparationGeneration() { }

    public static InterviewPreparationGeneration Create(CreateGenerationPayload payload)
    {
        return new InterviewPreparationGeneration
        {
            InterviewPreparationId = payload.InterviewPreparationId,
            TalentUserId = payload.TalentUserId,
            Kind = payload.Kind,
            CreatedAt = DateTime.UtcNow
        };
    }
}

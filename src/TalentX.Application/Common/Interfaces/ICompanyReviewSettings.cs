namespace TalentX.Application.Common.Interfaces;

public interface ICompanyReviewSettings
{
    /// <summary>Days after an online assessment's deadline before its review unlocks.</summary>
    int AssessmentReviewDelayDays { get; }
}

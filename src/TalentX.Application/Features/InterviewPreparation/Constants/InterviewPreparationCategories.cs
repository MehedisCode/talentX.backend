namespace TalentX.Application.Features.InterviewPreparation.Constants;

public static class InterviewPreparationCategories
{
    public const string Technical = "Technical";
    public const string Behavioral = "Behavioral";
    public const string ExperienceBased = "Experience-Based";
    public const string RoleSpecific = "Role-Specific";
    public const string Additional = "Additional";

    public static readonly IReadOnlyList<string> All =
    [
        Technical,
        Behavioral,
        ExperienceBased,
        RoleSpecific,
        Additional
    ];
}

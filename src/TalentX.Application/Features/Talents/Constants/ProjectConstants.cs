namespace TalentX.Application.Features.Talents.Constants;

public static class ProjectConstants
{
    public const int MaxProjectNameLength = 200;
    public const int MaxRoleLength = 150;
    public const int MaxProjectTypeLength = 50;
    public const int MaxStartDateLength = 50;
    public const int MaxEndDateLength = 50;
    public const int MaxDescriptionLength = 4000;
    public const int MaxProjectUrlLength = 500;
    public const int MaxTechnologiesCount = 30;
    public const int MaxTechnologyTagLength = 50;
    public static readonly string[] AllowedProjectTypes = ["Professional", "Personal", "Academic"];
}

namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record AdditionalQuestionsContext
{
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string TalentName { get; init; } = string.Empty;
    
    // Pass existing questions so AI doesn't duplicate them
    public string ExistingTechnicalQuestions { get; init; } = string.Empty;
    public string ExistingBehavioralQuestions { get; init; } = string.Empty;
    public string ExistingExperienceBasedQuestions { get; init; } = string.Empty;
    public string ExistingRoleSpecificQuestions { get; init; } = string.Empty;
}

namespace TalentX.Infrastructure.Mailing.Models;

public record InterviewConfirmationEmailModel(
    string RecipientName,
    string JobTitle,
    string CompanyName,
    string InterviewType,
    string LocationOrLink,
    string ScheduledTimeOrDeadline,
    string? Instructions);

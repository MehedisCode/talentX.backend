namespace TalentX.Infrastructure.Mailing.Models;

public sealed record JobAlertEmailModel(
    string RecipientName,
    string JobTitle,
    string CompanyName,
    string Location,
    string JobUrl,
    string AlertName
);

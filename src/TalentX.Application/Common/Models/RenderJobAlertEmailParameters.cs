namespace TalentX.Application.Common.Models;

public sealed record RenderJobAlertEmailParameters(
    string RecipientName,
    string JobTitle,
    string CompanyName,
    string Location,
    string JobUrl,
    string AlertName
);

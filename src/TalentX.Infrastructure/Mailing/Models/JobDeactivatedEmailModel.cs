namespace TalentX.Infrastructure.Mailing.Models;

public sealed record JobDeactivatedEmailModel(
    string RecipientName,
    string JobTitle,
    string ReasonLabel,
    string Details,
    string EditJobUrl
);

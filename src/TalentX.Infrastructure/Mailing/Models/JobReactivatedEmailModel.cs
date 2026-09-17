namespace TalentX.Infrastructure.Mailing.Models;

public sealed record JobReactivatedEmailModel(
    string RecipientName,
    string JobTitle,
    string JobUrl
);

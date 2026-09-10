namespace TalentX.Infrastructure.Mailing.Models;

public record JobInvitationEmailModel(string RecipientName, string JobTitle, string CompanyName, string JobUrl);

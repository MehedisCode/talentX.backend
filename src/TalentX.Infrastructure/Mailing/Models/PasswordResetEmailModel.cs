namespace TalentX.Infrastructure.Mailing.Models;

public record PasswordResetEmailModel(string RecipientName, string ResetUrl);

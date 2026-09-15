using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Mailing.Models;

public record AccountStatusChangedEmailModel(string RecipientName, AccountStatus NewStatus, UserRole Role);

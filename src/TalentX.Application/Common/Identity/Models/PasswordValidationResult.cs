namespace TalentX.Application.Common.Identity.Models;

public sealed class PasswordValidationResult
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required IList<string> Roles { get; init; }
}
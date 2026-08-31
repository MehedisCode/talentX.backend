namespace TalentX.Application.Identity.Models;

public sealed class LoginResult
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required TokenResponse Tokens { get; init; }
}
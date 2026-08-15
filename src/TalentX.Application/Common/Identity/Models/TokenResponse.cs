namespace TalentX.Application.Common.Identity.Models;

public sealed class TokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}
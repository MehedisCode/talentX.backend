namespace TalentX.Application.Features.Auth.CurrentUser;
public class CurrentUserResult
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
namespace TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;

public sealed record VerifyGuestEmailResponse(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    bool RequiresPasswordChange
);

namespace TalentX.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequestDto(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword);

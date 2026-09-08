namespace TalentX.Application.Features.Auth.AddPassword;

public sealed record AddPasswordRequestDto(
    string NewPassword,
    string ConfirmPassword,
    string? CurrentRefreshToken = null);

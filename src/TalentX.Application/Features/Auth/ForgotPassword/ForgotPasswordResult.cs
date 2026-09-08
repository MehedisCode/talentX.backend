namespace TalentX.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordResult(bool Success, string? Provider, string Message);

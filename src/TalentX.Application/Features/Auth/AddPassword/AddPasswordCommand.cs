using MediatR;

namespace TalentX.Application.Features.Auth.AddPassword;

public sealed record AddPasswordCommand(
    string NewPassword,
    string ConfirmPassword,
    string? CurrentRefreshToken = null) : IRequest<Unit>;

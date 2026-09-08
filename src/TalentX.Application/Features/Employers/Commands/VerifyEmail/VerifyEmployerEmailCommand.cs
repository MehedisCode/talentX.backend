using MediatR;
using TalentX.Application.DTOs.Auth;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

public sealed record VerifyEmployerEmailCommand(
    string Email,
    string Otp
) : IRequest<AuthResponseDto>;

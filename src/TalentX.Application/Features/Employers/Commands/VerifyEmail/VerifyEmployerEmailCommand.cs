using MediatR;
using TalentX.Application.DTOs.Employers;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

/// <summary>
/// Command to verify an employer's email using a 6-digit OTP code and issue JWT session tokens.
/// </summary>
/// <param name="Email">Employer email address.</param>
/// <param name="Otp">The 6-digit numeric OTP code.</param>
public sealed record VerifyEmployerEmailCommand(
    string Email,
    string Otp
) : IRequest<AuthEmployerDto>;

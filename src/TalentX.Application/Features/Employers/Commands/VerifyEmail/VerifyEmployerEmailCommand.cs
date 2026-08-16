using MediatR;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

/// <summary>
/// Command to verify an employer's email using a 6-digit OTP code.
/// </summary>
/// <param name="Email">Employer email address.</param>
/// <param name="Otp">The 6-digit numeric OTP code.</param>
public sealed record VerifyEmployerEmailCommand(
    string Email,
    string Otp
) : IRequest<bool>;

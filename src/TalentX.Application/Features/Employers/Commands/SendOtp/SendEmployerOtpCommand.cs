using MediatR;

namespace TalentX.Application.Features.Employers.Commands.SendOtp;

/// <summary>
/// Command to send a 6-digit OTP verification code to an employer's email address.
/// </summary>
/// <param name="Email">The email address to receive the verification OTP.</param>
public sealed record SendEmployerOtpCommand(string Email) : IRequest<bool>;

using MediatR;

namespace TalentX.Application.Features.Employers.Commands.ResendOtp;

/// <summary>
/// Command to request a new verification OTP email for an unverified employer account.
/// </summary>
/// <param name="Email">The registered employer's email address.</param>
public sealed record ResendEmployerOtpCommand(
    string Email
) : IRequest<bool>;

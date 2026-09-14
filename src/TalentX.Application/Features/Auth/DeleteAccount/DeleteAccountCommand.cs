using MediatR;

namespace TalentX.Application.Features.Auth.DeleteAccount;

/// <summary>
/// Result of a successful account deletion.
/// </summary>
/// <param name="Email">The email address of the deleted account.</param>
public sealed record DeleteAccountResult(string Email);

/// <summary>
/// Command to permanently delete the authenticated user's account after OTP verification.
/// The account, its owned data, and all referencing rows are hard-deleted.
/// </summary>
/// <param name="Otp">The 6-digit verification code sent to the user's email address.</param>
public sealed record DeleteAccountCommand(string Otp) : IRequest<DeleteAccountResult>;

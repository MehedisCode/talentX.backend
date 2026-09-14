using MediatR;

namespace TalentX.Application.Features.Auth.DeleteAccount;

/// <summary>
/// Command to send a 6-digit OTP verification code to the authenticated user's email
/// address as part of the account-deletion flow. Takes no email parameter — the
/// recipient is always the signed-in user themselves.
/// </summary>
public sealed record SendDeleteAccountOtpCommand : IRequest<bool>;

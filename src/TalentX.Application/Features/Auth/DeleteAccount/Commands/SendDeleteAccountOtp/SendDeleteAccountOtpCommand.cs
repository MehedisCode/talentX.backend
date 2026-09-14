using MediatR;

namespace TalentX.Application.Features.Auth.DeleteAccount.Commands.SendDeleteAccountOtp;

public sealed record SendDeleteAccountOtpCommand : IRequest<bool>;

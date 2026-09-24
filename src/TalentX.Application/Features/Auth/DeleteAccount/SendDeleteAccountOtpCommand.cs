using MediatR;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed record SendDeleteAccountOtpCommand : IRequest<bool>;

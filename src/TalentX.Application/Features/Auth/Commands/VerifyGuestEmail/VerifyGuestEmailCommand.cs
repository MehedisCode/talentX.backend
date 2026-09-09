using MediatR;

namespace TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;

public sealed record VerifyGuestEmailCommand(
    string Token
) : IRequest<VerifyGuestEmailResponse>;

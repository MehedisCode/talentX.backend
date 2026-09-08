using MediatR;
using TalentX.Application.DTOs.Auth;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

public sealed record GoogleSignInEmployerCommand(
    string IdToken
) : IRequest<AuthResponseDto>;

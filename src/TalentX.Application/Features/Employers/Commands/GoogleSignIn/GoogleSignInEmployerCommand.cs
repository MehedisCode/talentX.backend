using MediatR;
using TalentX.Application.DTOs.Employers;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

/// <summary>
/// Command to register or authenticate an employer account using a verified Google OAuth ID token.
/// </summary>
/// <param name="IdToken">The JWT ID token received from the Google Sign-In client.</param>
public sealed record GoogleSignInEmployerCommand(
    string IdToken
) : IRequest<EmployerDto>;

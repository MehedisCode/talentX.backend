using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.Login;
public record LoginCommand(string Email, string Password) 
    : IRequest<IdentityResponse<LoginResult>>;
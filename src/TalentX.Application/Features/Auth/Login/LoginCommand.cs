using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.LogIn;
public record LoginCommand(string Email, string Password) 
    : IRequest<IdentityResponse<LoginResult>>;
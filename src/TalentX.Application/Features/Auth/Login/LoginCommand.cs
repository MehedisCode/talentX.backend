using MediatR;
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Features.Auth.Login;
public record LoginCommand(string Email, string Password) 
    : IRequest<IdentityResponse<LoginResult>>;
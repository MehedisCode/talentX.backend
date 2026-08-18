using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) 
    : IRequest<IdentityResponse>;
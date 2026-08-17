using MediatR;
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) 
    : IRequest<IdentityResponse>;
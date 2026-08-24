using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) 
    : IRequest<IdentityResponse<TokenResponse>>;
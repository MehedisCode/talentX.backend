using MediatR;

namespace TalentX.Application.Features.Auth.ValidateResetToken;

public sealed record ValidateResetTokenQuery(string Email, string Token) : IRequest<ValidateResetTokenResult>;

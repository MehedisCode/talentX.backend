using MediatR;
using TalentX.Application.Identity.Interfaces;

namespace TalentX.Application.Features.Auth.ValidateResetToken;

public sealed class ValidateResetTokenQueryHandler(IIdentityService identityService)
    : IRequestHandler<ValidateResetTokenQuery, ValidateResetTokenResult>
{
    public async Task<ValidateResetTokenResult> Handle(
        ValidateResetTokenQuery query,
        CancellationToken cancellationToken)
    {
        var isValid = await identityService.ValidatePasswordResetTokenAsync(query.Email, query.Token, cancellationToken);
        return new ValidateResetTokenResult(isValid);
    }
}

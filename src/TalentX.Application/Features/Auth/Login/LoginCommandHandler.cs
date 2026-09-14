using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Auth.Login;

public class LoginCommandHandler 
    : IRequestHandler<LoginCommand, IdentityResponse<LoginResult>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IdentityResponse<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await _identityService.ValidatePasswordAsync(request.Email, request.Password);
        if (!validation.Success)
        {
            var failedAuditLog = new AuditLog(request.Email, "Login Failed", isSystem: false);
            await _unitOfWork.AuditLogs.AddAsync(failedAuditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return IdentityResponse<LoginResult>.Fail(validation.Errors);
        }

        var userId = validation.Data!.UserId;

        var tokenResult = await _tokenService.CreateAsync(userId);
        if (!tokenResult.Success)
        {
            var failedAuditLog = new AuditLog(request.Email, "Login Failed", isSystem: false);
            await _unitOfWork.AuditLogs.AddAsync(failedAuditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return IdentityResponse<LoginResult>.Fail(tokenResult.Errors);
        }

        var successAuditLog = new AuditLog(validation.Data.Email, "Login Success", isSystem: false);
        await _unitOfWork.AuditLogs.AddAsync(successAuditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return IdentityResponse<LoginResult>.Ok(new LoginResult
        {
            UserId = userId,
            Email = validation.Data.Email,
            Tokens = tokenResult.Data!
        });
    }
}
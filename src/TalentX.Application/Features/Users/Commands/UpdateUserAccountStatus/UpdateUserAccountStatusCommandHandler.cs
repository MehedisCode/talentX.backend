using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;

public sealed class UpdateUserAccountStatusCommandHandler : IRequestHandler<UpdateUserAccountStatusCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserAccountStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(UpdateUserAccountStatusCommand request, CancellationToken cancellationToken)
    {
        var actorEmail = _currentUserService.Email!;

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Role == UserRole.Admin)
        {
            throw new ForbiddenException("Admin accounts cannot be activated or deactivated here.");
        }

        if (request.NewStatus == AccountStatus.Active)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        _unitOfWork.Users.Update(user);

        var verb = request.NewStatus == AccountStatus.Active ? "Activated" : "Deactivated";
        var auditLog = new AuditLog(actorEmail, $"{verb} account {user.Email}");
        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto($"Account status updated to {request.NewStatus}.");
    }
}

using System.Text.Json;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.EmployerVerification.Commands.ReApply;

public sealed class ReApplyCommandHandler : IRequestHandler<ReApplyCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public ReApplyCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public async Task Handle(ReApplyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var employer = await _unitOfWork.Employers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Employer), userId);

        if (employer.VerificationStatus != EmployerVerificationStatus.Rejected)
            throw new ValidationException("Only rejected employers can re-apply for verification.");

        employer.ResetVerification();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishEmployerReAppliedNotificationsAsync(employer, cancellationToken);
    }

    private async Task PublishEmployerReAppliedNotificationsAsync(
        Domain.Employer employer,
        CancellationToken cancellationToken)
    {
        var adminUserIds = await _unitOfWork.Users.GetAdminUserIdsAsync(cancellationToken);

        var metadata = JsonSerializer.Serialize(new
        {
            employerId = employer.UserId,
            employerName = employer.Name,
            companyName = employer.CompanyName
        });

        foreach (var adminUserId in adminUserIds)
        {
            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    adminUserId,
                    NotificationType.EmployerReApplied,
                    "Employer Re-Application",
                    $"{employer.Name} from {employer.CompanyName} has re-applied for verification.",
                    metadata),
                cancellationToken);
        }
    }
}

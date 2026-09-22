using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.RejectEmployer;

public sealed class RejectEmployerCommandHandler : IRequestHandler<RejectEmployerCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public RejectEmployerCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task Handle(RejectEmployerCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByIdAsync(request.EmployerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Employer), request.EmployerId);

        if (employer.VerificationStatus != EmployerVerificationStatus.PendingVerification)
            throw new ValidationException("Only employers with Pending Verification status can be rejected.");

        employer.MarkAsRejected(request.Reason);

        var subject = "Your TalentX Employer Account Verification Update";
        var htmlBody = await _emailService.RenderEmployerRejectedEmailAsync(
            employer.Name,
            request.Reason,
            cancellationToken);

        await _unitOfWork.EmailDispatchLogs.AddAsync(
            new EmailDispatchLog(employer.Email, employer.Name, subject, htmlBody, "EmployerRejected"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.PublishAsync(
            new PublishNotificationParameters(
                employer.UserId,
                NotificationType.EmployerRejected,
                "Verification Rejected",
                $"Your employer verification was rejected. Reason: {request.Reason}",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    employerId = employer.UserId,
                    employerName = employer.Name,
                    companyName = employer.CompanyName,
                    rejectionReason = request.Reason
                })),
            cancellationToken);
    }
}

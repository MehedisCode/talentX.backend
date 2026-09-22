using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.ApproveEmployer;

public sealed class ApproveEmployerCommandHandler : IRequestHandler<ApproveEmployerCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public ApproveEmployerCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task Handle(ApproveEmployerCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByIdAsync(request.EmployerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Employer), request.EmployerId);

        if (employer.VerificationStatus != EmployerVerificationStatus.PendingVerification)
            throw new ValidationException("Only employers with Pending Verification status can be approved.");

        employer.MarkAsVerified();

        var subject = "Your TalentX Employer Account Has Been Approved";
        var htmlBody = await _emailService.RenderEmployerApprovedEmailAsync(
            employer.Name,
            cancellationToken);

        await _unitOfWork.EmailDispatchLogs.AddAsync(
            new EmailDispatchLog(employer.Email, employer.Name, subject, htmlBody, "EmployerApproved"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.PublishAsync(
            new PublishNotificationParameters(
                employer.UserId,
                NotificationType.EmployerApproved,
                "Account Verified",
                "Your employer account has been approved. You can now post jobs and recruit top talent.",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    employerId = employer.UserId,
                    employerName = employer.Name,
                    companyName = employer.CompanyName
                })),
            cancellationToken);
    }
}

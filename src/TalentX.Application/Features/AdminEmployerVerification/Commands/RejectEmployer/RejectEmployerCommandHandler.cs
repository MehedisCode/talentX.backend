using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.RejectEmployer;

public sealed class RejectEmployerCommandHandler : IRequestHandler<RejectEmployerCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public RejectEmployerCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
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
    }
}

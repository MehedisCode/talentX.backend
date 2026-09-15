using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.ApproveEmployer;

public sealed class ApproveEmployerCommandHandler : IRequestHandler<ApproveEmployerCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public ApproveEmployerCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task Handle(ApproveEmployerCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByIdAsync(request.EmployerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Employer), request.EmployerId);

        if (employer.VerificationStatus != EmployerVerificationStatus.PendingVerification)
            throw new ValidationException("Only employers with Pending Verification status can be approved.");

        employer.MarkAsVerified();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _ = Task.Run(() => _emailService.SendEmployerApprovedEmailAsync(
            employer.Email,
            employer.Name,
            cancellationToken), CancellationToken.None);
    }
}

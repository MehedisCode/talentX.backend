using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.EmployerVerification.Commands.ReApply;

public sealed class ReApplyCommandHandler : IRequestHandler<ReApplyCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ReApplyCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
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
    }
}

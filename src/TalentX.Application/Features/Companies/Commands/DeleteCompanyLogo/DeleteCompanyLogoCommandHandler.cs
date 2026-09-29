using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Companies.Commands.DeleteCompanyLogo;

public sealed class DeleteCompanyLogoCommandHandler : IRequestHandler<DeleteCompanyLogoCommand>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteCompanyLogoCommandHandler> _logger;

    public DeleteCompanyLogoCommandHandler(
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteCompanyLogoCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(DeleteCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated as an employer to remove your company logo.");

        // Existence check only — the profile itself is not needed further.
        if (await _unitOfWork.Employers.GetByUserIdAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException($"Employer profile for user '{userId}' was not found.");
        }

        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(userId, cancellationToken);

        // Idempotent: nothing to remove when there is no company or no logo.
        if (company is null || string.IsNullOrWhiteSpace(company.LogoStorageKey))
        {
            return;
        }

        try
        {
            await _fileStorageService.DeleteAsync(company.LogoStorageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete company logo {StorageKey} from storage", company.LogoStorageKey);
        }

        company.UpdateLogo(null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

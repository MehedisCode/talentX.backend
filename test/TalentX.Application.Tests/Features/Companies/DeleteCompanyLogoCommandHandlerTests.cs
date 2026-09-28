using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Companies.Commands.DeleteCompanyLogo;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class DeleteCompanyLogoCommandHandlerTests
{
    private const string UserId = "employer-user-123";
    private const string StorageKey = "company-logos/employer-user-123/logo.png";

    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly Mock<ILogger<DeleteCompanyLogoCommandHandler>> _loggerMock = new();

    public DeleteCompanyLogoCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Companies).Returns(_companyRepositoryMock.Object);
    }

    private DeleteCompanyLogoCommandHandler CreateHandler() =>
        new(
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

    private void SetupAuthenticatedUser() =>
        _currentUserServiceMock.Setup(u => u.UserId).Returns(UserId);

    private void SetupEmployer()
    {
        var employer = new Employer(UserId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);
    }

    private Company SetupCompanyWithLogo()
    {
        var company = new Company(UserId, "Acme Corp");
        company.UpdateLogo(StorageKey);
        _companyRepositoryMock
            .Setup(r => r.GetByEmployerIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        return company;
    }

    [Fact]
    public async Task Handle_DeletesLogoFromStorageAndClearsStorageKey()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        var company = SetupCompanyWithLogo();

        await CreateHandler().Handle(new DeleteCompanyLogoCommand(), CancellationToken.None);

        Assert.Null(company.LogoStorageKey);
        _fileStorageServiceMock.Verify(
            s => s.DeleteAsync(StorageKey, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SucceedsSilently_WhenCompanyHasNoLogo()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        var company = new Company(UserId, "Acme Corp");
        _companyRepositoryMock
            .Setup(r => r.GetByEmployerIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        await CreateHandler().Handle(new DeleteCompanyLogoCommand(), CancellationToken.None);

        _fileStorageServiceMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedException_WhenUserNotAuthenticated()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns((string?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new DeleteCompanyLogoCommand(), CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenEmployerProfileMissing()
    {
        SetupAuthenticatedUser();
        _employerRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateHandler().Handle(new DeleteCompanyLogoCommand(), CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

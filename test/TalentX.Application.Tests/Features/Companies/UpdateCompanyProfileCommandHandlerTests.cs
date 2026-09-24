using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Companies.Commands.UpdateCompanyProfile;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class UpdateCompanyProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlServiceMock = new();

    public UpdateCompanyProfileCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Companies).Returns(_companyRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_SuccessfullyUpdatesProfile_WhenValid()
    {
        var userId = "employer-user-123";
        _currentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var employer = new Employer(userId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var company = new Company(userId, "Acme Corp");
        _companyRepositoryMock.Setup(r => r.GetByEmployerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var command = new UpdateCompanyProfileCommand(
            "Acme Innovations Ltd",
            "Revolutionary widgets",
            "Manufacturing",
            "1000+",
            "https://acme.com",
            "New York, NY",
            "https://linkedin.com/company/acme",
            "https://twitter.com/acme",
            "https://facebook.com/acme"
        );

        var handler = new UpdateCompanyProfileCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Acme Innovations Ltd", result.Name);
        Assert.Equal("Revolutionary widgets", result.Description);
        Assert.Equal("Manufacturing", result.Industry);
        Assert.Equal("1000+", result.CompanySize);
        Assert.Equal("https://acme.com", result.Website);
        Assert.Equal("New York, NY", result.Location);
        Assert.Equal("https://linkedin.com/company/acme", result.LinkedIn);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedException_WhenUserNotAuthenticated()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns((string?)null);

        var command = new UpdateCompanyProfileCommand(
            "Acme Corp",
            null, null, null, null, null, null, null, null
        );

        var handler = new UpdateCompanyProfileCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object
        );

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenAnotherCompanyUsesTheName()
    {
        var userId = "employer-user-123";
        _currentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var employer = new Employer(userId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var company = new Company(userId, "Acme Corp");
        _companyRepositoryMock.Setup(r => r.GetByEmployerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var otherCompany = new Company("other-employer", "Acme Innovations Ltd");
        _companyRepositoryMock.Setup(r => r.GetByNameAsync("acme innovations ltd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCompany);

        var command = new UpdateCompanyProfileCommand(
            "  acme innovations ltd  ",
            null, null, null, null, null, null, null, null
        );

        var handler = new UpdateCompanyProfileCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object
        );

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Acme Innovations Ltd", exception.Message);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AllowsKeepingOwnCurrentName()
    {
        var userId = "employer-user-123";
        _currentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var employer = new Employer(userId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var company = new Company(userId, "Acme Corp");
        _companyRepositoryMock.Setup(r => r.GetByEmployerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _companyRepositoryMock.Setup(r => r.GetByNameAsync("Acme Corp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var command = new UpdateCompanyProfileCommand(
            "Acme Corp",
            null, null, null, null, null, null, null, null
        );

        var handler = new UpdateCompanyProfileCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Acme Corp", result.Name);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenCreatingWithTakenName()
    {
        var userId = "employer-user-123";
        _currentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var employer = new Employer(userId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _companyRepositoryMock.Setup(r => r.GetByEmployerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var otherCompany = new Company("other-employer", "Acme Corp");
        _companyRepositoryMock.Setup(r => r.GetByNameAsync("Acme Corp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCompany);

        var command = new UpdateCompanyProfileCommand(
            "Acme Corp",
            null, null, null, null, null, null, null, null
        );

        var handler = new UpdateCompanyProfileCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object
        );

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
    }
}

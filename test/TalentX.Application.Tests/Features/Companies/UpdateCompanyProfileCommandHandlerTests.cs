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
    private const string UserId = "employer-user-123";
    private const string ExpectedConflictMessage = "Company Name Already Exists.";

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

    // ───────────────────────── Helpers ─────────────────────────

    private UpdateCompanyProfileCommandHandler CreateHandler() =>
        new(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _presignedUrlServiceMock.Object);

    private static UpdateCompanyProfileCommand NameOnlyCommand(string name) =>
        new(name, null, null, null, null, null, null, null, null);

    private void SetupAuthenticatedUser() =>
        _currentUserServiceMock.Setup(u => u.UserId).Returns(UserId);

    private Employer SetupEmployer()
    {
        var employer = new Employer(UserId, "Jane Doe", "jane@acme.com", "Acme Corp");
        _employerRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);
        return employer;
    }

    private Company SetupExistingCompany(string name = "Acme Corp")
    {
        var company = new Company(UserId, name);
        _companyRepositoryMock
            .Setup(r => r.GetByEmployerIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        return company;
    }

    private void SetupNoExistingCompany() =>
        _companyRepositoryMock
            .Setup(r => r.GetByEmployerIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

    private void SetupNameOwner(string lookupName, Company? owner) =>
        _companyRepositoryMock
            .Setup(r => r.GetByNameAsync(lookupName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

    // ───────────────────────── Success ─────────────────────────

    [Fact]
    public async Task Handle_SuccessfullyUpdatesProfile_WhenValid()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        SetupExistingCompany();

        var command = new UpdateCompanyProfileCommand(
            "Acme Innovations Ltd",
            "Revolutionary widgets",
            "Manufacturing",
            "1000+",
            "https://acme.com",
            "New York, NY",
            "https://linkedin.com/company/acme",
            "https://twitter.com/acme",
            "https://facebook.com/acme");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Acme Innovations Ltd", result.Name);
        Assert.Equal("Revolutionary widgets", result.Description);
        Assert.Equal("Manufacturing", result.Industry);
        Assert.Equal("1000+", result.CompanySize);
        Assert.Equal("https://acme.com", result.Website);
        Assert.Equal("New York, NY", result.Location);
        Assert.Equal("https://linkedin.com/company/acme", result.LinkedIn);
        Assert.Equal("https://twitter.com/acme", result.Twitter);
        Assert.Equal("https://facebook.com/acme", result.Facebook);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrimsWhitespaceFromInputs()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        SetupExistingCompany();

        var command = new UpdateCompanyProfileCommand(
            "  Acme Innovations Ltd  ",
            "  Revolutionary widgets  ",
            "  Manufacturing  ",
            null, null, null, null, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("Acme Innovations Ltd", result.Name);
        Assert.Equal("Revolutionary widgets", result.Description);
        Assert.Equal("Manufacturing", result.Industry);

        // The uniqueness check must run against the trimmed name.
        _companyRepositoryMock.Verify(
            r => r.GetByNameAsync("Acme Innovations Ltd", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesCompany_WhenEmployerHasNone()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        SetupNoExistingCompany();
        SetupNameOwner("Acme Corp", null);

        var result = await CreateHandler().Handle(NameOnlyCommand("Acme Corp"), CancellationToken.None);

        Assert.Equal("Acme Corp", result.Name);
        _companyRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllowsKeepingOwnCurrentName()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        var company = SetupExistingCompany("Acme Corp");
        SetupNameOwner("Acme Corp", company);

        var result = await CreateHandler().Handle(NameOnlyCommand("Acme Corp"), CancellationToken.None);

        Assert.Equal("Acme Corp", result.Name);
        _companyRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───────────────────────── Failures ─────────────────────────

    [Fact]
    public async Task Handle_ThrowsUnauthorizedException_WhenUserNotAuthenticated()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns((string?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(NameOnlyCommand("Acme Corp"), CancellationToken.None));

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
            () => CreateHandler().Handle(NameOnlyCommand("Acme Corp"), CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenAnotherCompanyUsesTheName()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        SetupExistingCompany("Acme Corp");
        SetupNameOwner("acme innovations ltd", new Company("other-employer", "Acme Innovations Ltd"));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => CreateHandler().Handle(NameOnlyCommand("  acme innovations ltd  "), CancellationToken.None));

        Assert.Equal(ExpectedConflictMessage, exception.Message);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenCreatingWithTakenName()
    {
        SetupAuthenticatedUser();
        SetupEmployer();
        SetupNoExistingCompany();
        SetupNameOwner("Acme Corp", new Company("other-employer", "Acme Corp"));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => CreateHandler().Handle(NameOnlyCommand("Acme Corp"), CancellationToken.None));

        Assert.Equal(ExpectedConflictMessage, exception.Message);
        _companyRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
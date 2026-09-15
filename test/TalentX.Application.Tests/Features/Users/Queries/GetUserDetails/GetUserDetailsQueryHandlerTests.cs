using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Users.Queries.GetUserDetails;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Queries.GetUserDetails;

public sealed class GetUserDetailsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IEmployerRepository> _employerRepository = new();
    private readonly GetUserDetailsQueryHandler _handler;

    public GetUserDetailsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Employers).Returns(_employerRepository.Object);
        _handler = new GetUserDetailsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.Handle(new GetUserDetailsQuery { UserId = userId }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsAdmin_ThrowsNotFoundException()
    {
        // Arrange
        var admin = new User("admin-id", "Admin", "admin@talentx.com", UserRole.Admin, isVerified: true);
        _userRepository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        // Act
        var act = () => _handler.Handle(new GetUserDetailsQuery { UserId = admin.Id }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsTalent_EnrichesWithTalentDetails()
    {
        // Arrange
        var talent = new Talent("talent-id", "Jordan Smith", "jordan@talentx.com");
        talent.UpdateProfile(
            "Jordan Smith",
            tagline: "Senior Engineer",
            shortBio: "Bio",
            location: "Remote",
            phone: "123",
            linkedInUrl: "https://linkedin.com/in/jordan",
            additionalUrls: null,
            totalYearsExperience: 5m,
            expectedSalary: 100000m,
            skillPropositions: null);

        _userRepository.Setup(r => r.GetByIdAsync(talent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _talentRepository.Setup(r => r.GetByUserIdAsync(talent.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        // Act
        var result = await _handler.Handle(new GetUserDetailsQuery { UserId = talent.Id }, CancellationToken.None);

        // Assert
        result.Role.Should().Be(UserRole.Talent);
        result.TalentDetails.Should().NotBeNull();
        result.TalentDetails!.Tagline.Should().Be("Senior Engineer");
        result.EmployerDetails.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsEmployer_EnrichesWithEmployerDetails()
    {
        // Arrange
        var employer = new Employer("employer-id", "Employer Name", "employer@talentx.com", "Acme Inc");

        _userRepository.Setup(r => r.GetByIdAsync(employer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);
        _employerRepository.Setup(r => r.GetByUserIdAsync(employer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(employer);

        // Act
        var result = await _handler.Handle(new GetUserDetailsQuery { UserId = employer.Id }, CancellationToken.None);

        // Assert
        result.Role.Should().Be(UserRole.Employer);
        result.EmployerDetails.Should().NotBeNull();
        result.EmployerDetails!.CompanyName.Should().Be("Acme Inc");
        result.Status.Should().Be(AccountStatus.Active);
        result.TalentDetails.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsGuest_ReturnsBaseFieldsWithoutThrowing()
    {
        // Arrange
        var guest = new User("guest-id", "Guest User", "guest@talentx.com", UserRole.Guest);
        _userRepository.Setup(r => r.GetByIdAsync(guest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(guest);

        // Act
        var result = await _handler.Handle(new GetUserDetailsQuery { UserId = guest.Id }, CancellationToken.None);

        // Assert
        result.Role.Should().Be(UserRole.Guest);
        result.TalentDetails.Should().BeNull();
        result.EmployerDetails.Should().BeNull();
    }
}

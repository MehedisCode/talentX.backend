using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpdateTalentProfile;

public class UpdateTalentProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateTalentProfileCommandHandler _handler;

    public UpdateTalentProfileCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new UpdateTalentProfileCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static UpdateTalentProfileCommand ValidCommand() => new(
        "Jane Doe",
        "Dhaka",
        "Backend Engineer",
        "Five years of backend experience.",
        "+8801000000000",
        "https://linkedin.com/in/janedoe",
        new List<string> { "https://portfolio.example.com" },
        5m,
        120000m,
        new List<string> { "Scalable systems" }
    );

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithAnExistingTalent_UpdatesAllProfileFields()
    {
        var talent = new Talent("user-1", "Old Name", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        var command = ValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        talent.Name.Should().Be(command.Name);
        talent.Location.Should().Be(command.Location);
        talent.Tagline.Should().Be(command.Tagline);
        talent.ShortBio.Should().Be(command.ShortBio);
        talent.Phone.Should().Be(command.Phone);
        talent.LinkedInUrl.Should().Be(command.LinkedInUrl);
        talent.AdditionalUrls.Should().BeEquivalentTo(command.AdditionalUrls);
        talent.TotalYearsExperience.Should().Be(command.TotalYearsExperience);
        talent.ExpectedSalary.Should().Be(command.ExpectedSalary);
        talent.SkillPropositions.Should().BeEquivalentTo(command.SkillPropositions);
    }

    [Fact]
    public async Task Handle_WithAnExistingTalent_CallsSaveChangesAsyncExactlyOnce()
    {
        var talent = new Talent("user-1", "Old Name", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _talentRepository.Verify(r => r.Update(talent), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

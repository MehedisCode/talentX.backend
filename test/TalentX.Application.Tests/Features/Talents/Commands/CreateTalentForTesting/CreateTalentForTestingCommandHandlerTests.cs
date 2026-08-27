using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.CreateTalentForTesting;

public class CreateTalentForTestingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IHostEnvironmentService> _hostEnvironmentService = new();
    private readonly CreateTalentForTestingCommandHandler _handler;

    public CreateTalentForTestingCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _hostEnvironmentService.SetupGet(e => e.IsDevelopment).Returns(true);
        _handler = new CreateTalentForTestingCommandHandler(_unitOfWork.Object, _hostEnvironmentService.Object);
    }

    private static CreateTalentForTestingCommand ValidCommand() => new(
        "user-1",
        "Jane Doe",
        "jane@example.com"
    );

    [Fact]
    public async Task Handle_WhenNoTalentExistsForUser_PersistsANewTalent()
    {
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        Talent? addedTalent = null;
        _talentRepository
            .Setup(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()))
            .Callback<Talent, CancellationToken>((talent, _) => addedTalent = talent)
            .Returns(Task.CompletedTask);

        var command = ValidCommand();

        var talentId = await _handler.Handle(command, CancellationToken.None);

        addedTalent.Should().NotBeNull();
        addedTalent!.Id.Should().Be(talentId);
        addedTalent.UserId.Should().Be(command.UserId);
        addedTalent.Name.Should().Be(command.Name);
        addedTalent.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_WhenNoTalentExistsForUser_CallsSaveChangesAsyncExactlyOnce()
    {
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenATalentAlreadyExistsForUser_ThrowsConflictException()
    {
        var existingTalent = new Talent("user-1", "Existing Talent", "existing@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTalent);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenATalentAlreadyExistsForUser_DoesNotPersistOrSave()
    {
        var existingTalent = new Talent("user-1", "Existing Talent", "existing@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTalent);

        try
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);
        }
        catch (ConflictException)
        {
            // expected — assertions below verify no side effects occurred.
        }

        _talentRepository.Verify(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotInDevelopment_ThrowsForbiddenException()
    {
        _hostEnvironmentService.SetupGet(e => e.IsDevelopment).Returns(false);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenNotInDevelopment_DoesNotPersistOrSave()
    {
        _hostEnvironmentService.SetupGet(e => e.IsDevelopment).Returns(false);

        try
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);
        }
        catch (ForbiddenException)
        {
            // expected — assertions below verify no side effects occurred.
        }

        _talentRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _talentRepository.Verify(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

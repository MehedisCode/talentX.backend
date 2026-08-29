using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ApplyExtractedProfile;

public class ApplyExtractedProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ApplyExtractedProfileCommandHandler _handler;

    public ApplyExtractedProfileCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");

        _handler = new ApplyExtractedProfileCommandHandler(
            _unitOfWork.Object,
            _fileStorageService.Object,
            _currentUserService.Object);
    }

    private static ApplyExtractedProfileCommand ValidCommand() => new(
        ["C#", ".NET"],
        ["Communication"],
        ["Git"],
        [new ApplyExtractedWorkExperienceItem("Acme", "Engineer", "2020", "Present", true)],
        [new ApplyExtractedEducationItem("State University", "BSc", "2016", "2020")],
        "resumes/user-1/new-key.pdf",
        "resume.pdf",
        "application/pdf",
        1024
    );

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenResumeStorageKeyBelongsToAnotherUser_ThrowsNotFoundException_AndTouchesNoRepository()
    {
        var command = ValidCommand() with { ResumeStorageKey = "resumes/other-user/stolen-key.pdf" };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _resumeRepository.Verify(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _resumeRepository.Verify(r => r.Update(It.IsAny<Resume>()), Times.Never);
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResumeStorageKeyContainsDotDotSegment_ThrowsNotFoundException()
    {
        var command = ValidCommand() with { ResumeStorageKey = "resumes/user-1/../other-user/stolen-key.pdf" };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNoResumeRowExists_InsertsNewResumeRow()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _resumeRepository.Verify(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Once);
        _resumeRepository.Verify(r => r.Update(It.IsAny<Resume>()), Times.Never);
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResumeRowAlreadyExists_UpdatesExistingRowInstead()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        var existingResume = new Resume("user-1", "resumes/user-1/old-key.pdf", "old.pdf", "application/pdf", 512);
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResume);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _resumeRepository.Verify(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        _resumeRepository.Verify(r => r.Update(existingResume), Times.Once);
        existingResume.StorageKey.Should().Be("resumes/user-1/new-key.pdf");
    }

    [Fact]
    public async Task Handle_WhenResumeRowAlreadyExists_DeletesOldStorageObjectOnlyAfterSaveSucceeds()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        var existingResume = new Resume("user-1", "resumes/user-1/old-key.pdf", "old.pdf", "application/pdf", 512);
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResume);

        var callOrder = new List<string>();
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);
        _fileStorageService
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync("resumes/user-1/old-key.pdf", It.IsAny<CancellationToken>()), Times.Once);
        callOrder.Should().Equal("save", "delete");
    }

    [Fact]
    public async Task Handle_WhenSaveChangesAsyncThrows_NeverDeletesOldStorageObject()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        var existingResume = new Resume("user-1", "resumes/user-1/old-key.pdf", "old.pdf", "application/pdf", 512);
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResume);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNewKeyEqualsOldKey_DoesNotDeleteTheObject()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        var existingResume = new Resume("user-1", "resumes/user-1/new-key.pdf", "old.pdf", "application/pdf", 512);
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResume);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReplacesWorkExperiencesAndEducationsForUser()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _workExperienceRepository.Verify(r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _workExperienceRepository.Verify(r => r.AddAsync(It.Is<WorkExperience>(w => w.CompanyName == "Acme" && w.IsCurrent), It.IsAny<CancellationToken>()), Times.Once);
        _educationRepository.Verify(r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _educationRepository.Verify(r => r.AddAsync(It.Is<Education>(e => e.Institution == "State University"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesTalentSkills_AndSavesOnce()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        talent.TechnicalSkills.Should().BeEquivalentTo(["C#", ".NET"]);
        talent.SoftSkills.Should().BeEquivalentTo(["Communication"]);
        talent.ToolsAndPlatforms.Should().BeEquivalentTo(["Git"]);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

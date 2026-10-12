using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<ApplyExtractedProfileCommandHandler>> _logger = new();
    private readonly ApplyExtractedProfileCommandHandler _handler;

    public ApplyExtractedProfileCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _unitOfWork.SetupGet(u => u.Projects).Returns(_projectRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new ApplyExtractedProfileCommandHandler(
            _unitOfWork.Object,
            _fileStorageService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    private static Mock<IUploadedFile> ValidFileMock()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        return fileMock;
    }

    private static ApplyExtractedProfileData ValidProfileData() => new(
        "Jane Smith",
        "Senior Cloud Architect",
        "Experienced architect with expertise in cloud systems.",
        "San Francisco, CA",
        "+14155552671",
        "https://www.linkedin.com/in/janesmith",
        ["https://github.com/janesmith"],
        8.5m,
        140000m,
        ["High velocity delivery"],
        ["C#", ".NET"],
        ["Communication"],
        ["Git"],
        [new ApplyExtractedWorkExperienceItem("Acme", "Engineer", "2020", "Present", true)],
        [new ApplyExtractedEducationItem("State University", "BSc", "2016", "2020")],
        [new ApplyExtractedProjectItem("Alpha", "Lead", "Professional", "2023", null, true, "Desc", ["C#"], "https://example.com")]
    );

    private static ApplyExtractedProfileCommand ValidCommand(IUploadedFile file) => new(
        ValidProfileData(),
        file
    );

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException_AndTouchesNoStorage()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _fileStorageService.Verify(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SavesResumeFile_ToPermanentResumesKeyScopedToUser()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        string? capturedStorageFileName = null;
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IUploadedFile, string, CancellationToken>((_, storageFileName, _) => capturedStorageFileName = storageFileName)
            .ReturnsAsync((IUploadedFile _, string storageFileName, CancellationToken _) => storageFileName);

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        capturedStorageFileName.Should().NotBeNull();
        capturedStorageFileName.Should().StartWith("resumes/user-1/");
        capturedStorageFileName.Should().EndWith("_resume.pdf");

        _resumeRepository.Verify(
            r => r.AddAsync(It.Is<Resume>(resume => resume.StorageKey.StartsWith("resumes/user-1/")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SavesFileBeforeCommittingToDatabase()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        var callOrder = new List<string>();
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync("resumes/user-1/new-key.pdf");
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("commit"))
            .ReturnsAsync(1);

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        callOrder.Should().Equal("save", "commit");
    }

    [Fact]
    public async Task Handle_WhenFileSaveFails_ThrowsBadGatewayException_AndMakesNoDatabaseChanges()
    {
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));

        var act = () => _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _talentRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesAsyncThrows_DeletesTheJustSavedNewFile_BeforeRethrowing()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var act = () => _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _fileStorageService.Verify(s => s.DeleteAsync("resumes/user-1/new-key.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNoResumeRowExists_InsertsNewResumeRow()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

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
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        _resumeRepository.Verify(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Never);
        existingResume.StorageKey.Should().Be("resumes/user-1/new-key.pdf");
    }

    [Fact]
    public async Task Handle_WhenResumeRowAlreadyExists_DeletesOldStorageObjectOnlyAfterSaveSucceeds()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        var existingResume = new Resume("user-1", "resumes/user-1/old-key.pdf", "old.pdf", "application/pdf", 512);
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingResume);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        var callOrder = new List<string>();
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);
        _fileStorageService
            .Setup(s => s.DeleteAsync("resumes/user-1/old-key.pdf", It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync("resumes/user-1/old-key.pdf", It.IsAny<CancellationToken>()), Times.Once);
        callOrder.Should().Equal("save", "delete");
    }

    [Fact]
    public async Task Handle_ReplacesWorkExperiencesAndEducationsForUser()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        _workExperienceRepository.Verify(r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _workExperienceRepository.Verify(r => r.AddAsync(It.Is<WorkExperience>(w => w.CompanyName == "Acme" && w.IsCurrent), It.IsAny<CancellationToken>()), Times.Once);
        _educationRepository.Verify(r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _educationRepository.Verify(r => r.AddAsync(It.Is<Education>(e => e.Institution == "State University"), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepository.Verify(r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _projectRepository.Verify(r => r.AddAsync(It.Is<Project>(p => p.ProjectName == "Alpha" && p.IsOngoing), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesTalentSkills_AndSavesOnce()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        talent.TechnicalSkills.Should().BeEquivalentTo(["C#", ".NET"]);
        talent.SoftSkills.Should().BeEquivalentTo(["Communication"]);
        talent.ToolsAndPlatforms.Should().BeEquivalentTo(["Git"]);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesTalentProfile_WithProvidedValues()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        await _handler.Handle(ValidCommand(ValidFileMock().Object), CancellationToken.None);

        talent.Name.Should().Be("Jane Smith");
        talent.Tagline.Should().Be("Senior Cloud Architect");
        talent.ShortBio.Should().Be("Experienced architect with expertise in cloud systems.");
        talent.Location.Should().Be("San Francisco, CA");
        talent.Phone.Should().Be("+14155552671");
        talent.LinkedInUrl.Should().Be("https://www.linkedin.com/in/janesmith");
        talent.AdditionalUrls.Should().Equal("https://github.com/janesmith");
        talent.TotalYearsExperience.Should().Be(8.5m);
        talent.ExpectedSalary.Should().Be(140000m);
        talent.SkillPropositions.Should().Equal("High velocity delivery");
    }

    [Fact]
    public async Task Handle_WhenProfileFieldsAreNull_PreservesExistingTalentProfileValues()
    {
        var talent = new Talent("user-1", "Original Name", "jane@example.com");
        talent.UpdateProfile(
            "Original Name",
            "Original Tagline",
            "Original Bio",
            "Original Location",
            "Original Phone",
            "https://linkedin.com/in/original",
            ["https://original.com"],
            3m,
            80000m,
            ["Original Proposition"]);

        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        var sparseProfileData = new ApplyExtractedProfileData(
            FullName: null,
            Tagline: null,
            ShortBio: null,
            Location: null,
            Phone: null,
            LinkedInUrl: null,
            AdditionalUrls: null,
            TotalYearsExperience: null,
            ExpectedSalary: null,
            SkillPropositions: null,
            TechnicalSkills: null,
            SoftSkills: null,
            ToolsAndPlatforms: null,
            WorkExperiences: null,
            Educations: null);

        var command = new ApplyExtractedProfileCommand(sparseProfileData, ValidFileMock().Object);

        await _handler.Handle(command, CancellationToken.None);

        talent.Name.Should().Be("Original Name");
        talent.Tagline.Should().Be("Original Tagline");
        talent.ShortBio.Should().Be("Original Bio");
        talent.Location.Should().Be("Original Location");
        talent.Phone.Should().Be("Original Phone");
        talent.LinkedInUrl.Should().Be("https://linkedin.com/in/original");
        talent.AdditionalUrls.Should().Equal("https://original.com");
        talent.TotalYearsExperience.Should().Be(3m);
        talent.ExpectedSalary.Should().Be(80000m);
        talent.SkillPropositions.Should().Equal("Original Proposition");
    }

    [Fact]
    public async Task Handle_WhenFullNameIsWhitespace_PreservesExistingTalentName()
    {
        var talent = new Talent("user-1", "Original Name", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _resumeRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/new-key.pdf");

        var profileData = ValidProfileData() with { FullName = "   " };
        var command = new ApplyExtractedProfileCommand(profileData, ValidFileMock().Object);

        await _handler.Handle(command, CancellationToken.None);

        talent.Name.Should().Be("Original Name");
    }

    [Fact]
    public async Task Handle_WhenResumeFileIsNull_UpdatesProfileAndChildCollectionsWithoutTouchingResumeStorageOrEntity()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        var command = new ApplyExtractedProfileCommand(ValidProfileData(), null);

        await _handler.Handle(command, CancellationToken.None);

        _fileStorageService.Verify(
            s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _resumeRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _resumeRepository.Verify(
            r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _fileStorageService.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        talent.Name.Should().Be("Jane Smith");
        talent.Tagline.Should().Be("Senior Cloud Architect");
        talent.TechnicalSkills.Should().BeEquivalentTo(["C#", ".NET"]);

        _workExperienceRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _workExperienceRepository.Verify(
            r => r.AddAsync(It.IsAny<WorkExperience>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _educationRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _educationRepository.Verify(
            r => r.AddAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _projectRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _projectRepository.Verify(
            r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Resumes.Commands.SaveResume;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class SaveResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<IResumeExtractionService> _resumeExtractionService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<SaveResumeCommandHandler>> _logger = new();
    private readonly SaveResumeCommandHandler _handler;

    public SaveResumeCommandHandlerTests()
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
        _resumeRepository
            .Setup(r => r.CountByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IUploadedFile _, string storageFileName, CancellationToken _) => storageFileName);

        _handler = new SaveResumeCommandHandler(
            _unitOfWork.Object,
            _fileStorageService.Object,
            _resumeExtractionService.Object,
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

    [Fact]
    public async Task Handle_SavesFirstResumeAsDefault_UnderUserScopedStorageKey()
    {
        Resume? added = null;
        _resumeRepository
            .Setup(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .Callback<Resume, CancellationToken>((resume, _) => added = resume)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: false),
            CancellationToken.None);

        added.Should().NotBeNull();
        added!.IsDefault.Should().BeTrue();
        added.DisplayName.Should().Be("resume");
        added.StorageKey.Should().StartWith("resumes/user-1/");
        added.StorageKey.Should().EndWith("_resume.pdf");
        result.IsDefault.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _resumeExtractionService.Verify(
            s => s.ExtractAsync(It.IsAny<IUploadedFile>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithExplicitDisplayName_UsesIt()
    {
        Resume? added = null;
        _resumeRepository
            .Setup(r => r.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .Callback<Resume, CancellationToken>((resume, _) => added = resume)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, "Backend Resume", UpdateProfile: false),
            CancellationToken.None);

        added!.DisplayName.Should().Be("Backend Resume");
        result.DisplayName.Should().Be("Backend Resume");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasFiveResumes_ThrowsConflict_AndTouchesNoStorage()
    {
        _resumeRepository
            .Setup(r => r.CountByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resume.MaxPerUser);

        var act = () => _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _fileStorageService.Verify(
            s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFileSaveFails_ThrowsBadGateway_AndDoesNotPersist()
    {
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));

        var act = () => _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_DeletesStoredFileAndRethrows()
    {
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var act = () => _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _fileStorageService.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUpdateProfile_AppliesExtractionToTalentAndChildRecords()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _resumeExtractionService
            .Setup(s => s.ExtractAsync(It.IsAny<IUploadedFile>(), ResumeExtractionPurpose.CandidateProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumeExtractionResult
            {
                FullName = "Jane Extraction",
                Tagline = "Extracted Tagline",
                ShortBio = "Extracted Bio",
                Location = "New York, NY",
                Phone = "+12125551234",
                LinkedInUrl = "https://linkedin.com/in/janeextracted",
                PortfolioUrls = ["https://extracted.dev"],
                TotalYearsExperience = 6m,
                ExpectedSalary = 110000m,
                SkillPropositions = ["Quick learner"],
                TechnicalSkills = ["C#"],
                SoftSkills = ["Communication"],
                ToolsAndPlatforms = ["Git"],
                WorkExperience =
                [
                    new WorkExperienceDto
                    {
                        CompanyName = "Acme",
                        JobTitle = "Engineer",
                        StartDate = "2020",
                        EndDate = "Present",
                        IsCurrent = true
                    }
                ],
                Education =
                [
                    new EducationDto
                    {
                        Institution = "State University",
                        Degree = "BSc",
                        StartDate = "2016",
                        EndDate = "2020"
                    }
                ],
                Projects =
                [
                    new ExtractedProjectDto
                    {
                        ProjectName = "TalentX Platform",
                        Role = "Backend Engineer",
                        ProjectType = "Professional",
                        StartDate = "2023",
                        EndDate = "Present",
                        IsOngoing = true,
                        Description = "AI Platform",
                        TechnologiesUsed = ["C#", ".NET"],
                        ProjectUrl = "https://talentx.ai"
                    }
                ]
            });

        var result = await _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: true),
            CancellationToken.None);

        result.IsDefault.Should().BeTrue();
        talent.Name.Should().Be("Jane Extraction");
        talent.Tagline.Should().Be("Extracted Tagline");
        talent.ShortBio.Should().Be("Extracted Bio");
        talent.Location.Should().Be("New York, NY");
        talent.Phone.Should().Be("+12125551234");
        talent.LinkedInUrl.Should().Be("https://linkedin.com/in/janeextracted");
        talent.AdditionalUrls.Should().Equal("https://extracted.dev");
        talent.TotalYearsExperience.Should().Be(6m);
        talent.ExpectedSalary.Should().Be(110000m);
        talent.SkillPropositions.Should().Equal("Quick learner");
        talent.TechnicalSkills.Should().Equal("C#");
        talent.SoftSkills.Should().Equal("Communication");
        talent.ToolsAndPlatforms.Should().Equal("Git");
        _workExperienceRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _workExperienceRepository.Verify(
            r => r.AddAsync(
                It.Is<WorkExperience>(w => w.CompanyName == "Acme" && w.JobTitle == "Engineer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _educationRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _educationRepository.Verify(
            r => r.AddAsync(
                It.Is<Education>(e => e.Institution == "State University"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _projectRepository.Verify(
            r => r.DeleteByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _projectRepository.Verify(
            r => r.AddAsync(
                It.Is<Project>(p => p.ProjectName == "TalentX Platform" && p.Role == "Backend Engineer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUpdateProfile_WhenTalentAlreadyHasProfileFields_OverridesExistingProfileFields()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        talent.UpdateProfile(
            "Jane Doe",
            "Existing Tagline",
            "Existing Bio",
            "Existing Location",
            "Existing Phone",
            "https://linkedin.com/in/existing",
            ["https://existing.dev"],
            10m,
            150000m,
            ["Existing Proposition"]);

        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _resumeExtractionService
            .Setup(s => s.ExtractAsync(It.IsAny<IUploadedFile>(), ResumeExtractionPurpose.CandidateProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumeExtractionResult
            {
                FullName = "Extracted Name",
                Tagline = "Extracted Tagline",
                ShortBio = "Extracted Bio",
                Location = "Extracted Location",
                Phone = "Extracted Phone",
                LinkedInUrl = "https://linkedin.com/in/extracted",
                PortfolioUrls = ["https://extracted.dev"],
                TotalYearsExperience = 2m,
                ExpectedSalary = 50000m,
                SkillPropositions = ["Extracted Proposition"],
                TechnicalSkills = ["Python"]
            });

        await _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: true),
            CancellationToken.None);

        talent.Name.Should().Be("Extracted Name");
        talent.Tagline.Should().Be("Extracted Tagline");
        talent.ShortBio.Should().Be("Extracted Bio");
        talent.Location.Should().Be("Extracted Location");
        talent.Phone.Should().Be("Extracted Phone");
        talent.LinkedInUrl.Should().Be("https://linkedin.com/in/extracted");
        talent.AdditionalUrls.Should().Equal("https://extracted.dev");
        talent.TotalYearsExperience.Should().Be(2m);
        talent.ExpectedSalary.Should().Be(50000m);
        talent.SkillPropositions.Should().Equal("Extracted Proposition");
        talent.TechnicalSkills.Should().Equal("Python");
        talent.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Handle_WhenExtractionFails_ThrowsBadGateway_AndDoesNotSaveFile()
    {
        _resumeExtractionService
            .Setup(s => s.ExtractAsync(It.IsAny<IUploadedFile>(), ResumeExtractionPurpose.CandidateProfile, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("AI down"));

        var act = () => _handler.Handle(
            new SaveResumeCommand(ValidFileMock().Object, null, UpdateProfile: true),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _fileStorageService.Verify(
            s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

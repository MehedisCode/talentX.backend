using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentProfile;

public class GetTalentProfileQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly IMapper _mapper;
    private readonly GetTalentProfileQueryHandler _handler;

    public GetTalentProfileQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<WorkExperience, WorkExperienceItemDto>();
            cfg.CreateMap<Education, EducationItemDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();

        _handler = new GetTalentProfileQueryHandler(
            _unitOfWork.Object,
            _presignedUrlService.Object,
            _currentUserService.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetTalentProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("missing-user");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("missing-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(new GetTalentProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTalentHasNoStorageKeys_ReturnsNullDownloadUrls()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());
        _resumeRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume?)null);

        var response = await _handler.Handle(new GetTalentProfileQuery(), CancellationToken.None);

        response.ProfilePictureUrl.Should().BeNull();
        response.VideoIntroUrl.Should().BeNull();
        response.ResumeUrl.Should().BeNull();
        response.ResumeOriginalFileName.Should().BeNull();
        _presignedUrlService.Verify(
            s => s.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTalentHasStorageKeysAndChildRecords_ReturnsPopulatedProfile()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-2");
        var talent = new Talent("user-2", "John Smith", "john@example.com");
        talent.UpdateProfilePicture("profile-pictures/user-2.png");
        talent.UpdateVideoIntro("videos/user-2.mp4");

        var workExperience = new WorkExperience("user-2", "Acme Corp", "Engineer", "2020", "Present", true);
        var education = new Education("user-2", "State University", "B.Sc. Computer Science", "2016", "2020");
        var resume = new Resume("user-2", "resumes/user-2.pdf", "resume.pdf", "application/pdf", 1024);

        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience> { workExperience });
        _educationRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education> { education });
        _resumeRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);
        _presignedUrlService
            .Setup(s => s.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string storageKey, TimeSpan _, CancellationToken _) => $"https://signed.example.com/{storageKey}");

        var response = await _handler.Handle(new GetTalentProfileQuery(), CancellationToken.None);

        response.ProfilePictureUrl.Should().Be("https://signed.example.com/profile-pictures/user-2.png");
        response.VideoIntroUrl.Should().Be("https://signed.example.com/videos/user-2.mp4");
        response.ResumeUrl.Should().Be("https://signed.example.com/resumes/user-2.pdf");
        response.ResumeOriginalFileName.Should().Be("resume.pdf");
        response.WorkExperiences.Should().ContainSingle(item => item.CompanyName == "Acme Corp");
        response.Educations.Should().ContainSingle(item => item.Institution == "State University");
    }
}

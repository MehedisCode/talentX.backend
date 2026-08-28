using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentApplicationPrefill;

public class GetTalentApplicationPrefillQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly IMapper _mapper;
    private readonly GetTalentApplicationPrefillQueryHandler _handler;

    public GetTalentApplicationPrefillQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<WorkExperience, WorkExperienceItemDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();

        _handler = new GetTalentApplicationPrefillQueryHandler(
            _unitOfWork.Object,
            _presignedUrlService.Object,
            _currentUserService.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetTalentApplicationPrefillQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("missing-user");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("missing-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(new GetTalentApplicationPrefillQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForAFreshTalentWithNoResumeOrExperience_ReturnsAWellFormedEmptyPayload()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _resumeRepository
            .Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume?)null);

        var response = await _handler.Handle(new GetTalentApplicationPrefillQuery(), CancellationToken.None);

        response.Name.Should().Be("Jane Doe");
        response.Email.Should().Be("jane@example.com");
        response.Phone.Should().BeNull();
        response.Location.Should().BeNull();
        response.ExpectedSalary.Should().BeNull();
        response.TotalYearsExperience.Should().BeNull();
        response.ResumeStorageKey.Should().BeNull();
        response.ResumeOriginalFileName.Should().BeNull();
        response.ResumeUrl.Should().BeNull();
        response.WorkExperiences.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ForATalentWithDataAndAResume_ReturnsAPopulatedPayload()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-2");
        var talent = new Talent("user-2", "John Smith", "john@example.com");
        talent.UpdateProfile(
            "John Smith",
            "Backend Engineer",
            "Bio",
            "Dhaka",
            "+8801000000000",
            null,
            null,
            5m,
            120000m,
            null);

        var workExperience = new WorkExperience("user-2", "Acme Corp", "Engineer", "2020", "Present", true);
        var resume = new Resume("user-2", "resumes/user-2.pdf", "resume.pdf", "application/pdf", 1024);

        _talentRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience> { workExperience });
        _resumeRepository
            .Setup(r => r.GetByUserIdAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);
        _presignedUrlService
            .Setup(s => s.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string storageKey, TimeSpan _, CancellationToken _) => $"https://signed.example.com/{storageKey}");

        var response = await _handler.Handle(new GetTalentApplicationPrefillQuery(), CancellationToken.None);

        response.Location.Should().Be("Dhaka");
        response.ExpectedSalary.Should().Be(120000m);
        response.TotalYearsExperience.Should().Be(5m);
        response.ResumeStorageKey.Should().Be("resumes/user-2.pdf");
        response.ResumeOriginalFileName.Should().Be("resume.pdf");
        response.ResumeUrl.Should().Be("https://signed.example.com/resumes/user-2.pdf");
        response.WorkExperiences.Should().ContainSingle(item => item.CompanyName == "Acme Corp");
    }
}

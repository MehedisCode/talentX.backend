using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentProfileForEmployer;

public class GetTalentProfileForEmployerQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly IMapper _mapper;
    private readonly GetTalentProfileForEmployerQueryHandler _handler;

    public GetTalentProfileForEmployerQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);

        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");

        _presignedUrlService
            .Setup(p => p.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, TimeSpan expiry, CancellationToken _) => $"https://storage.test/{key}?expiry={expiry.TotalMinutes}");

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<WorkExperience, WorkExperienceItemDto>();
            cfg.CreateMap<Education, EducationItemDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();

        _handler = new GetTalentProfileForEmployerQueryHandler(
            _unitOfWork.Object,
            _presignedUrlService.Object,
            _currentUserService.Object,
            _mapper);
    }

    private static Job CreateJob(string employerId)
    {
        return Job.Create(
            employerId,
            "Senior Frontend Developer",
            "Acme Inc",
            "Job description text",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "React", "TypeScript" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static JobApplication CreateApplication(Guid jobId, string talentUserId)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Jane Candidate",
            "jane@example.com",
            "resumes/app_resume.pdf",
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");
    }

    private static Talent CreateTalent(string userId)
    {
        var talent = new Talent(userId, "Jane Candidate", "jane@example.com");
        talent.UpdateProfile(
            name: "Jane Candidate",
            tagline: "Senior Full Stack Dev",
            shortBio: "Passionate engineer with 5+ years experience.",
            location: "New York, USA",
            phone: "+1234567890",
            linkedInUrl: "https://linkedin.com/in/janecandidate",
            additionalUrls: new List<string> { "https://github.com/janecandidate" },
            totalYearsExperience: 5.5m,
            expectedSalary: 110000m,
            skillPropositions: new List<string> { "React expert", ".NET core" });

        talent.UpdateProfilePicture("pictures/jane.jpg");
        talent.UpdateVideoIntro("videos/jane_intro.mp4");
        talent.UpdateSkills(
            new List<string> { "React", "TypeScript", "C#" },
            new List<string> { "Leadership", "Communication" },
            new List<string> { "Docker", "AWS" });
        return talent;
    }

    [Fact]
    public async Task Handle_WhenCallerIsEmployerWhoOwnsJob_ReturnsDetailedPublicProfile()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-1");
        application.SetAiMatchScore(86m);
        var talent = CreateTalent("talent-1");

        var workExp = new WorkExperience(
            "talent-1", "Tech Corp", "Lead Dev", "2021-01-01", null, true);

        var education = new Education(
            "talent-1", "MIT", "B.Sc. Computer Science", "2017-09-01", "2021-06-01");

        var resume = new Resume("talent-1", "resumes/profile_cv.pdf", "Official_Resume.pdf", "application/pdf", 1024);

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience> { workExp });
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education> { education });
        _resumeRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(resume);

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ApplicationId.Should().Be(application.Id);
        result.JobId.Should().Be(job.Id);
        result.TalentUserId.Should().Be("talent-1");
        result.Name.Should().Be("Jane Candidate");
        result.Email.Should().Be("jane@example.com");
        result.Tagline.Should().Be("Senior Full Stack Dev");
        result.ShortBio.Should().Be("Passionate engineer with 5+ years experience.");
        result.Location.Should().Be("New York, USA");
        result.LinkedInUrl.Should().Be("https://linkedin.com/in/janecandidate");
        result.AdditionalUrls.Should().Contain("https://github.com/janecandidate");
        result.TechnicalSkills.Should().Contain(new[] { "React", "TypeScript", "C#" });
        result.WorkExperiences.Should().HaveCount(1);
        result.WorkExperiences.First().CompanyName.Should().Be("Tech Corp");
        result.Educations.Should().HaveCount(1);
        result.Educations.First().Institution.Should().Be("MIT");
        result.ProfilePictureUrl.Should().Be("https://storage.test/pictures/jane.jpg?expiry=60");
        result.VideoIntroUrl.Should().Be("https://storage.test/videos/jane_intro.mp4?expiry=60");
        result.ResumeUrl.Should().Be("https://storage.test/resumes/app_resume.pdf?expiry=60");
        result.ResumeOriginalFileName.Should().Be("Official_Resume.pdf");
        result.AiMatchScore.Should().Be(86m);
        result.CoverLetter.Should().Be("Cover letter text");
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthorized_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var query = new GetTalentProfileForEmployerQuery(Guid.NewGuid());

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be authenticated to view candidate profile.");
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        _jobApplicationRepository.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync((JobApplication?)null);

        var query = new GetTalentProfileForEmployerQuery(appId);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job application '{appId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenEmployerDoesNotOwnJob_ThrowsForbiddenException()
    {
        var job = CreateJob("other-employer");
        var application = CreateApplication(job.Id, "talent-1");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to view candidate profile for this job.");
    }

    [Fact]
    public async Task Handle_WhenTalentRecordIsMissing_FallsBackToApplicationDetails()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-missing");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-missing", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-missing", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-missing", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetByUserIdAsync("talent-missing", It.IsAny<CancellationToken>())).ReturnsAsync((Resume?)null);

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Name.Should().Be(application.ApplicantName);
        result.Email.Should().Be(application.ApplicantEmail);
        result.Location.Should().Be(application.Location);
        result.TotalYearsExperience.Should().Be(application.TotalYearsExperience);
        result.ExpectedSalary.Should().Be(application.ExpectedSalary);
        result.ResumeOriginalFileName.Should().Be("app_resume.pdf");
    }
}

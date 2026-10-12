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
    private readonly Mock<IProjectRepository> _projectRepository = new();
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
        _unitOfWork.SetupGet(u => u.Projects).Returns(_projectRepository.Object);
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);

        _projectRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

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

        var project = new Project(
            "talent-1",
            "TalentX Platform",
            "Lead Architect",
            "Commercial",
            "2023-01-01",
            null,
            true,
            "Built core microservices and AI pipelines",
            new List<string> { ".NET 10", "Next.js", "PostgreSQL" },
            "https://talentx.ai");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience> { workExp });
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education> { education });
        _projectRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project> { project });
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume> { resume });

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
        result.Projects.Should().HaveCount(1);
        var firstProject = result.Projects.First();
        firstProject.ProjectName.Should().Be("TalentX Platform");
        firstProject.Role.Should().Be("Lead Architect");
        firstProject.ProjectType.Should().Be("Commercial");
        firstProject.IsOngoing.Should().BeTrue();
        firstProject.TechnologiesUsed.Should().Contain(new[] { ".NET 10", "Next.js", "PostgreSQL" });
        firstProject.ProjectUrl.Should().Be("https://talentx.ai");
        result.ProfilePictureUrl.Should().Be("https://storage.test/pictures/jane.jpg?expiry=60");
        result.VideoIntroUrl.Should().Be("https://storage.test/videos/jane_intro.mp4?expiry=60");
        result.ResumeUrl.Should().Be("https://storage.test/resumes/app_resume.pdf?expiry=60");
        result.ResumeOriginalFileName.Should().Be("app_resume.pdf");
        result.ResumeKind.Should().Be("PDF");
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
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-missing", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume>());

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Name.Should().Be(application.ApplicantName);
        result.Email.Should().Be(application.ApplicantEmail);
        result.Location.Should().Be(application.Location);
        result.TotalYearsExperience.Should().Be(application.TotalYearsExperience);
        result.ExpectedSalary.Should().Be(application.ExpectedSalary);
        result.ResumeOriginalFileName.Should().Be("app_resume.pdf");
    }

    [Fact]
    public async Task Handle_WhenMultipleExperiencesAndEducationsExist_ReturnsThemInReverseChronologicalOrder()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-multi");
        var talent = CreateTalent("talent-multi");

        var workExperiences = new List<WorkExperience>
        {
            new("talent-multi", "Company A", "Senior Software Test Engineer", "04/2022", "09/2025", false),
            new("talent-multi", "Company B", "SQA Engineer (Old)", "04/2019", "01/2020", false),
            new("talent-multi", "Company C", "Quality Excellence Lead", "09/2025", "Present", true),
            new("talent-multi", "Company D", "SQA Engineer (Mid)", "02/2021", "02/2022", false)
        };

        var educations = new List<Education>
        {
            new("talent-multi", "State University", "B.Sc. Computer Science", "2016", "2020"),
            new("talent-multi", "Research Institute", "Ph.D.", "2023", "Present"),
            new("talent-multi", "Graduate School", "M.Sc. Software Engineering", "2020", "2022")
        };

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-multi", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-multi", It.IsAny<CancellationToken>())).ReturnsAsync(workExperiences);
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-multi", It.IsAny<CancellationToken>())).ReturnsAsync(educations);
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-multi", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume>());

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.WorkExperiences.Select(item => item.JobTitle).Should().Equal(
            "Quality Excellence Lead",
            "Senior Software Test Engineer",
            "SQA Engineer (Mid)",
            "SQA Engineer (Old)");

        result.Educations.Select(item => item.Degree).Should().Equal(
            "Ph.D.",
            "M.Sc. Software Engineering",
            "B.Sc. Computer Science");
    }

    [Fact]
    public async Task Handle_WhenMultipleProjectsExist_ReturnsThemInReverseChronologicalOrder()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-projects");
        var talent = CreateTalent("talent-projects");

        var projects = new List<Project>
        {
            new("talent-projects", "Legacy Portal", "Developer", "Client", "2019", "2020", false, "Description", new List<string>(), null),
            new("talent-projects", "Current Flagship", "Lead", "Internal", "2024", null, true, "Description", new List<string>(), null),
            new("talent-projects", "Recent App", "Engineer", "OpenSource", "2022", "2023", false, "Description", new List<string>(), null)
        };

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-projects", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-projects", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-projects", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _projectRepository.Setup(r => r.GetByUserIdAsync("talent-projects", It.IsAny<CancellationToken>())).ReturnsAsync(projects);
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-projects", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume>());

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Projects.Select(project => project.ProjectName).Should().Equal(
            "Current Flagship",
            "Recent App",
            "Legacy Portal");
    }

    [Fact]
    public async Task Handle_WhenApplicationUsesSavedResume_ShowsItsDisplayName()
    {
        var job = CreateJob("employer-1");
        var prefixGuid = Guid.NewGuid();
        var sharedKey = $"resumes/talent-1/{prefixGuid}_candidate_resume.pdf";
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            sharedKey,
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");
        var talent = CreateTalent("talent-1");
        var resume = new Resume(
            "talent-1",
            sharedKey,
            $"{prefixGuid}_candidate_resume.pdf",
            "application/pdf",
            1024,
            "Backend Resume");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume> { resume });

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeOriginalFileName.Should().Be("Backend Resume");
        result.ResumeKind.Should().Be("PDF");
        result.ResumeUrl.Should().Be($"https://storage.test/{sharedKey}?expiry=60");
    }

    [Fact]
    public async Task Handle_WhenApplicationUsesNonDefaultSavedResume_ShowsThatResumesDisplayName()
    {
        var job = CreateJob("employer-1");
        var defaultResume = new Resume(
            "talent-1", "resumes/talent-1/default_cv.pdf", "Profile_CV.pdf", "application/pdf", 1024,
            "Default Resume", isDefault: true);
        var usedResume = new Resume(
            "talent-1", "resumes/talent-1/used_cv.pdf", "Used_CV.pdf", "application/pdf", 2048,
            "Backend Resume");
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            "resumes/talent-1/used_cv.pdf",
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");
        var talent = CreateTalent("talent-1");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume> { defaultResume, usedResume });

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeOriginalFileName.Should().Be("Backend Resume");
        result.ResumeKind.Should().Be("PDF");
        result.ResumeUrl.Should().Be("https://storage.test/resumes/talent-1/used_cv.pdf?expiry=60");
    }

    [Fact]
    public async Task Handle_WhenSavedResumeIsDocxWithGenericMime_ReturnsDocxKind()
    {
        var job = CreateJob("employer-1");
        var prefixGuid = Guid.NewGuid();
        var sharedKey = $"resumes/talent-1/{prefixGuid}_Naib Uddin CV Resume.docx";
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            sharedKey,
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");
        var talent = CreateTalent("talent-1");
        var resume = new Resume(
            "talent-1",
            sharedKey,
            $"{prefixGuid}_Naib Uddin CV Resume.docx",
            "application/octet-stream",
            2048,
            "Md_Mehedi_Hasan_Rafi_Senior_Software_Engineer_Full_Stack_DotNet_NextJS_React_TypeScript_JavaScript_P");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume> { resume });

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeKind.Should().Be("DOCX");
        result.ResumeOriginalFileName.Should().Be(
            "Md_Mehedi_Hasan_Rafi_Senior_Software_Engineer_Full_Stack_DotNet_NextJS_React_TypeScript_JavaScript_P");
    }

    [Fact]
    public async Task Handle_WhenApplicationHasNoResumeStorageKey_FallsBackToProfileResume()
    {
        var job = CreateJob("employer-1");
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            string.Empty,
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");
        var talent = CreateTalent("talent-1");
        var resume = new Resume(
            "talent-1", "resumes/profile_cv.pdf", "Official_Resume.pdf", "application/pdf", 1024,
            isDefault: true);

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume> { resume });

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeOriginalFileName.Should().Be("Official_Resume.pdf");
        result.ResumeKind.Should().Be("PDF");
        result.ResumeUrl.Should().Be("https://storage.test/resumes/profile_cv.pdf?expiry=60");
    }

    [Fact]
    public async Task Handle_WhenResumeIsNullAndApplicationResumeStorageKeyHasGuidPrefix_StripsGuidPrefix()
    {
        var job = CreateJob("employer-1");
        var prefixGuid = Guid.NewGuid();
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            $"resumes/pending/{prefixGuid}_app_resume.pdf",
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume>());

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeOriginalFileName.Should().Be("app_resume.pdf");
    }

    [Fact]
    public async Task Handle_WhenBothResumeAndApplicationResumeStorageKeyAreNullOrEmpty_ReturnsDefaultResumePdf()
    {
        var job = CreateJob("employer-1");
        var application = new JobApplication(
            job.Id,
            "talent-1",
            "Jane Candidate",
            "jane@example.com",
            string.Empty,
            "Cover letter text",
            100000m,
            5m,
            "New York, USA");

        _jobApplicationRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _talentRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);
        _workExperienceRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkExperience>());
        _educationRepository.Setup(r => r.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Education>());
        _resumeRepository.Setup(r => r.GetAllByUserIdAsync("talent-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resume>());

        var query = new GetTalentProfileForEmployerQuery(application.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ResumeOriginalFileName.Should().Be("Resume.pdf");
        result.ResumeKind.Should().BeNull();
    }
}

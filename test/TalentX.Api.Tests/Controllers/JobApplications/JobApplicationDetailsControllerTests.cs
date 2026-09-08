using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.JobApplications;
using TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.JobApplications;

public class JobApplicationDetailsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobApplicationDetailsController _controller;

    public JobApplicationDetailsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new JobApplicationDetailsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    private static JobApplicationDetailsDto Dto(Guid id) => new(
        id,
        Guid.NewGuid(),
        "Jane Talent",
        "jane@example.com",
        "resumes/abc_cv.pdf",
        "https://s3.amazonaws.com/test-bucket/resumes/abc_cv.pdf?signature",
        "I am a great fit.",
        90000m,
        5m,
        "Dhaka",
        "Applied",
        85m,
        DateTime.UtcNow,
        null);

    [Fact]
    public async Task GetById_SendsQueryWithTheRouteId()
    {
        var id = Guid.NewGuid();
        _sender
            .Setup(s => s.Send(It.Is<GetJobApplicationByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dto(id));

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(
            s => s.Send(It.Is<GetJobApplicationByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WithTheDto()
    {
        var id = Guid.NewGuid();
        var dto = Dto(id);
        _sender
            .Setup(s => s.Send(It.IsAny<GetJobApplicationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task UpdateStatus_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        var id = Guid.NewGuid();
        var requestDto = new UpdateStatusRequestDto(ApplicationStatus.Shortlisted);
        var expectedResponse = new StatusUpdateResponseDto("Application status updated to Shortlisted.");

        _sender
            .Setup(s => s.Send(It.Is<UpdateApplicationStatusCommand>(c => c.ApplicationId == id && c.NewStatus == ApplicationStatus.Shortlisted), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.UpdateStatus(id, requestDto, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResponse);
        _sender.Verify(
            s => s.Send(It.Is<UpdateApplicationStatusCommand>(c => c.ApplicationId == id && c.NewStatus == ApplicationStatus.Shortlisted), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCandidateProfile_SendsQueryToMediator_ReturnsOkWithProfile()
    {
        var id = Guid.NewGuid();
        var profileResponse = new TalentPublicProfileResponse(
            ApplicationId: id,
            JobId: Guid.NewGuid(),
            TalentUserId: "talent-1",
            Name: "Jane Candidate",
            Email: "jane@example.com",
            Tagline: "Dev",
            ShortBio: "Bio",
            Location: "Dhaka",
            LinkedInUrl: null,
            AdditionalUrls: new List<string>(),
            TotalYearsExperience: 5m,
            ExpectedSalary: 100000m,
            SkillPropositions: new List<string>(),
            TechnicalSkills: new List<string> { "C#" },
            SoftSkills: new List<string>(),
            ToolsAndPlatforms: new List<string>(),
            ProfilePictureUrl: null,
            VideoIntroUrl: null,
            ResumeOriginalFileName: "Resume.pdf",
            ResumeUrl: "https://storage.test/resume.pdf",
            ApplicationStatus: "Applied",
            CoverLetter: null,
            AppliedAt: DateTime.UtcNow,
            WorkExperiences: new List<WorkExperienceItemDto>(),
            Educations: new List<EducationItemDto>()
        );

        _sender
            .Setup(s => s.Send(It.Is<TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer.GetTalentProfileForEmployerQuery>(q => q.ApplicationId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileResponse);

        var result = await _controller.GetCandidateProfile(id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(profileResponse);
    }
}


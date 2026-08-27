using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Talents;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Talents;

public class TalentsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IWebHostEnvironment> _webHostEnvironment = new();

    private TalentsController CreateController()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        return new TalentsController(_currentUserService.Object, _webHostEnvironment.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public async Task GetMyProfile_WhenCallerIsAuthenticated_SendsQueryAndReturnsOk()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        var expectedResponse = new TalentProfileResponse(
            Guid.NewGuid(), "user-1", "Jane Doe", "jane@example.com",
            null, null, null, null, null, new List<string>(),
            null, null, new List<string>(), new List<string>(), new List<string>(), new List<string>(),
            null, null, null, null,
            new List<WorkExperienceItemDto>(), new List<EducationItemDto>(),
            DateTime.UtcNow, null);

        _sender
            .Setup(s => s.Send(It.IsAny<GetTalentProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = CreateController();

        var actionResult = await controller.GetMyProfile(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        _sender.Verify(
            s => s.Send(It.Is<GetTalentProfileQuery>(q => q.UserId == "user-1"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var controller = CreateController();

        var act = () => controller.GetMyProfile(CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task UpdateMyProfile_SendsUpdateTalentProfileCommand_AndReturnsNoContent()
    {
        var command = new UpdateTalentProfileCommand(
            "Jane Doe", "Dhaka", "Backend Engineer", "Bio", "+8801000000000",
            "https://linkedin.com/in/janedoe", null, 5m, 120000m, null);

        var controller = CreateController();

        var actionResult = await controller.UpdateMyProfile(command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyApplicationPrefill_WhenCallerIsAuthenticated_SendsQueryAndReturnsOk()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        var expectedResponse = new TalentApplicationPrefillResponse(
            "Jane Doe", "jane@example.com", null, null, null, null, null, null, null,
            new List<WorkExperienceItemDto>());

        _sender
            .Setup(s => s.Send(It.IsAny<GetTalentApplicationPrefillQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = CreateController();

        var actionResult = await controller.GetMyApplicationPrefill(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateTalentForTesting_InDevelopment_SendsCommandAndReturnsCreated()
    {
        _webHostEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var command = new CreateTalentForTestingCommand("user-1", "Jane Doe", "jane@example.com");
        var talentId = Guid.NewGuid();

        _sender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talentId);

        var controller = CreateController();

        var actionResult = await controller.CreateTalentForTesting(command, CancellationToken.None);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTalentForTesting_OutsideDevelopment_ReturnsNotFoundAndNeverSendsCommand()
    {
        _webHostEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var command = new CreateTalentForTestingCommand("user-1", "Jane Doe", "jane@example.com");
        var controller = CreateController();

        var actionResult = await controller.CreateTalentForTesting(command, CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundResult>();
        _sender.Verify(s => s.Send(It.IsAny<CreateTalentForTestingCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

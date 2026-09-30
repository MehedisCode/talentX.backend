using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Talents;
using TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;
using TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Talents;

public class InterviewPreparationControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly InterviewPreparationController _controller;

    public InterviewPreparationControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new InterviewPreparationController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    private static InterviewPreparationDto Preparation()
    {
        return new InterviewPreparationDto
        {
            Id = Guid.NewGuid(),
            InterviewInvitationId = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            JobTitle = "Backend Engineer",
            CompanyName = "Acme",
            InterviewType = "OnCall",
            Status = "Generated",
            GenerationCount = 1,
        };
    }

    [Fact]
    public async Task GetMyPreparations_SendsQueryAndReturnsOkWithPreparations()
    {
        var expected = new List<InterviewPreparationDto> { Preparation(), Preparation() };
        _sender
            .Setup(s => s.Send(It.IsAny<GetPreparationsByTalentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actionResult = await _controller.GetMyPreparations(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(
            s => s.Send(It.IsAny<GetPreparationsByTalentQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPreparation_SendsQueryWithInvitationIdAndReturnsOk()
    {
        var invitationId = Guid.NewGuid();
        var expected = Preparation();
        _sender
            .Setup(s => s.Send(It.IsAny<GetPreparationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actionResult = await _controller.GetPreparation(invitationId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(
            s => s.Send(
                It.Is<GetPreparationQuery>(q => q.InvitationId == invitationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePreparation_SendsCommandWithInvitationIdAndReturnsOk()
    {
        var invitationId = Guid.NewGuid();
        var expected = Preparation();
        _sender
            .Setup(s => s.Send(It.IsAny<GeneratePreparationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actionResult = await _controller.GeneratePreparation(invitationId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(
            s => s.Send(
                It.Is<GeneratePreparationCommand>(c => c.InvitationId == invitationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegeneratePreparation_SendsCommandWithInvitationIdAndReturnsOk()
    {
        var invitationId = Guid.NewGuid();
        var expected = Preparation();
        _sender
            .Setup(s => s.Send(It.IsAny<RegeneratePreparationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actionResult = await _controller.RegeneratePreparation(invitationId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(
            s => s.Send(
                It.Is<RegeneratePreparationCommand>(c => c.InvitationId == invitationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateMoreQuestions_SendsCommandWithInvitationIdAndReturnsOk()
    {
        var invitationId = Guid.NewGuid();
        var expected = Preparation();
        _sender
            .Setup(s => s.Send(It.IsAny<GenerateAdditionalQuestionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actionResult = await _controller.GenerateMoreQuestions(invitationId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(
            s => s.Send(
                It.Is<GenerateAdditionalQuestionsCommand>(c => c.InvitationId == invitationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Controller_IsRestrictedToTalents()
    {
        var authorize = typeof(InterviewPreparationController)
            .GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be("Talent");
    }

    [Theory]
    [InlineData(nameof(InterviewPreparationController.GeneratePreparation))]
    [InlineData(nameof(InterviewPreparationController.RegeneratePreparation))]
    [InlineData(nameof(InterviewPreparationController.GenerateMoreQuestions))]
    public void GenerateEndpoints_AreRateLimited(string methodName)
    {
        var method = typeof(InterviewPreparationController).GetMethod(methodName);

        method.Should().NotBeNull();
        var rateLimit = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        rateLimit.Should().NotBeNull();
        rateLimit!.PolicyName.Should().Be("InterviewPrepGenerate");
    }
}

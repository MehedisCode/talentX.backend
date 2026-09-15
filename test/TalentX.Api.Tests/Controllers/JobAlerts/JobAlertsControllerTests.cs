using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.JobAlerts;
using TalentX.API.Controllers.JobAlerts.Requests;
using TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.JobAlerts.Queries.GetJobAlerts;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.JobAlerts;

public class JobAlertsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobAlertsController _controller;

    public JobAlertsControllerTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_sender.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _controller = new JobAlertsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_WithTalentRole()
    {
        var authorizeAttribute = typeof(JobAlertsController)
            .GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Theory]
    [InlineData(nameof(JobAlertsController.CreateJobAlert))]
    [InlineData(nameof(JobAlertsController.GetJobAlerts))]
    [InlineData(nameof(JobAlertsController.UpdateJobAlert))]
    [InlineData(nameof(JobAlertsController.ToggleJobAlert))]
    [InlineData(nameof(JobAlertsController.DeleteJobAlert))]
    public void Actions_HaveAuthorizeAttribute_WithTalentRole(string actionMethodName)
    {
        var methodInfo = typeof(JobAlertsController).GetMethod(actionMethodName);

        methodInfo.Should().NotBeNull();
        var authorizeAttribute = methodInfo!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public async Task CreateJobAlert_ValidRequest_SendsCommandAndReturns201CreatedWithAlertId()
    {
        var createRequest = new CreateJobAlertRequest(
            "Backend Alerts",
            "Senior Backend Engineer",
            "Dhaka",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            WorkArrangement.FullRemote);

        var expectedAlertId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send(It.IsAny<CreateJobAlertCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAlertId);

        var actionResult = await _controller.CreateJobAlert(createRequest, CancellationToken.None);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be(expectedAlertId);

        _sender.Verify(
            sender => sender.Send(
                It.Is<CreateJobAlertCommand>(command =>
                    command.Parameters.Name == createRequest.Name &&
                    command.Parameters.Title == createRequest.Title &&
                    command.Parameters.Location == createRequest.Location &&
                    command.Parameters.EmploymentType == createRequest.EmploymentType &&
                    command.Parameters.ExperienceLevel == createRequest.ExperienceLevel &&
                    command.Parameters.WorkArrangement == createRequest.WorkArrangement),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetJobAlerts_Returns200Ok_WithListOfJobAlerts()
    {
        var expectedAlerts = new List<JobAlertDto>
        {
            new(
                Guid.NewGuid(),
                "Frontend Alerts",
                "Frontend Engineer",
                "Remote",
                EmploymentType.FullTime,
                ExperienceLevel.MidLevel,
                WorkArrangement.FullRemote,
                true,
                DateTime.UtcNow),
            new(
                Guid.NewGuid(),
                "DevOps Alerts",
                "DevOps Engineer",
                null,
                EmploymentType.Contract,
                ExperienceLevel.SeniorLevel,
                WorkArrangement.Hybrid,
                false,
                DateTime.UtcNow)
        };

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetJobAlertsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAlerts);

        var actionResult = await _controller.GetJobAlerts(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedAlerts);
        _sender.Verify(sender => sender.Send(It.IsAny<GetJobAlertsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAlert_ValidRequest_SendsCommandWithRouteIdAndReturns204NoContent()
    {
        var alertId = Guid.NewGuid();
        var updateRequest = new UpdateJobAlertRequest(
            "Updated Alerts",
            "Lead Backend Engineer",
            "Dhaka",
            EmploymentType.FullTime,
            ExperienceLevel.Lead,
            WorkArrangement.Hybrid);

        _sender
            .Setup(sender => sender.Send(It.IsAny<UpdateJobAlertCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.UpdateJobAlert(alertId, updateRequest, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<UpdateJobAlertCommand>(command =>
                    command.Id == alertId &&
                    command.Parameters.Name == updateRequest.Name &&
                    command.Parameters.Title == updateRequest.Title &&
                    command.Parameters.Location == updateRequest.Location &&
                    command.Parameters.EmploymentType == updateRequest.EmploymentType &&
                    command.Parameters.ExperienceLevel == updateRequest.ExperienceLevel &&
                    command.Parameters.WorkArrangement == updateRequest.WorkArrangement),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ToggleJobAlert_ValidRequest_SendsCommandWithRouteIdAndReturns204NoContent()
    {
        var alertId = Guid.NewGuid();
        var toggleRequest = new ToggleJobAlertRequest(false);

        _sender
            .Setup(sender => sender.Send(It.IsAny<ToggleJobAlertCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.ToggleJobAlert(alertId, toggleRequest, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<ToggleJobAlertCommand>(command => command.Id == alertId && !command.IsEnabled),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteJobAlert_ValidId_SendsCommandWithRouteIdAndReturns204NoContent()
    {
        var alertId = Guid.NewGuid();

        _sender
            .Setup(sender => sender.Send(It.IsAny<DeleteJobAlertCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.DeleteJobAlert(alertId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<DeleteJobAlertCommand>(command => command.Id == alertId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

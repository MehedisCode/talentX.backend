using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.API.Controllers.Admin;
using TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;
using TalentX.Application.Features.AiConfiguration.DTOs;
using TalentX.Application.Features.AiConfiguration.Queries.GetAiConfiguration;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class AdminAiConfigurationControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly AdminAiConfigurationController _controller;

    public AdminAiConfigurationControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new AdminAiConfigurationController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void Controller_InheritsBaseApiController()
    {
        typeof(AdminAiConfigurationController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void Controller_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(AdminAiConfigurationController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/admin/ai-configuration");
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttribute = typeof(AdminAiConfigurationController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public async Task GetConfiguration_ReturnsOk_WithConfigurationDto()
    {
        var expectedDto = new AiConfigurationDto(
            ModelName: "claude-3-7-sonnet-20250219",
            MaskedApiKey: "sk-...1234",
            HasCustomKey: true,
            UpdatedAt: DateTime.UtcNow);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAiConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var response = await _controller.GetConfiguration(CancellationToken.None);

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _sender.Verify(
            sender => sender.Send(It.IsAny<GetAiConfigurationQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConfiguration_ReturnsOk_WithUpdatedConfigurationDto()
    {
        var request = new UpdateAiConfigRequest("gpt-4o", "sk-proj-testkey12345678");
        var expectedDto = new AiConfigurationDto(
            ModelName: "gpt-4o",
            MaskedApiKey: "sk-...5678",
            HasCustomKey: true,
            UpdatedAt: DateTime.UtcNow);

        _sender
            .Setup(sender => sender.Send(
                It.Is<UpdateAiConfigurationCommand>(command =>
                    command.ModelName == request.ModelName &&
                    command.ApiKey == request.ApiKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var response = await _controller.UpdateConfiguration(request, CancellationToken.None);

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _sender.Verify(
            sender => sender.Send(
                It.Is<UpdateAiConfigurationCommand>(command =>
                    command.ModelName == request.ModelName &&
                    command.ApiKey == request.ApiKey),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

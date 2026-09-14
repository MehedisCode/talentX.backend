using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.PreferredJobs;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.PreferredJobs;

public class PreferredJobsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly PreferredJobsController _controller;

    public PreferredJobsControllerTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_sender.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _controller = new PreferredJobsController
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
        var authorizeAttribute = typeof(PreferredJobsController)
            .GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public void GetPreferredJobs_HasAuthorizeAttribute_WithTalentRole()
    {
        var methodInfo = typeof(PreferredJobsController).GetMethod(nameof(PreferredJobsController.GetPreferredJobs));

        methodInfo.Should().NotBeNull();
        var authorizeAttribute = methodInfo!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public async Task GetPreferredJobs_ValidParameters_SendsQueryAndReturns200OkWithPaginatedJobs()
    {
        var alertId = Guid.NewGuid();
        var queryParameters = new PreferredJobsQueryParams(alertId, 1, 10);
        var jobItems = new List<JobListItemDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Full Stack Engineer",
                CompanyName = "Acme Tech",
                Location = "Dhaka",
                City = "Dhaka",
                Country = "Bangladesh",
                EmploymentType = "FullTime",
                ExperienceLevel = "Senior",
                WorkArrangement = "Hybrid",
                RequiredSkills = new List<string> { "C#", "React" },
                CreatedAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddDays(30)
            }
        };

        var expectedPaginatedList = new PaginatedList<JobListItemDto>(
            jobItems,
            queryParameters.Page,
            queryParameters.PageSize,
            jobItems.Count);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetPreferredJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPaginatedList);

        var actionResult = await _controller.GetPreferredJobs(queryParameters, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedPaginatedList);

        _sender.Verify(
            sender => sender.Send(
                It.Is<GetPreferredJobsQuery>(query => query.Parameters == queryParameters),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.API.Controllers.Employers;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Employers;

public sealed class EmployerCompanyReviewsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly EmployerCompanyReviewsController _controller;

    public EmployerCompanyReviewsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new EmployerCompanyReviewsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void EmployerCompanyReviewsController_Class_InheritsBaseApiController()
    {
        typeof(EmployerCompanyReviewsController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void EmployerCompanyReviewsController_Class_RequiresEmployerRoleForEveryAction()
    {
        // Declared once on the class, so it covers every action.
        var authorizeAttribute = typeof(EmployerCompanyReviewsController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Employer));
    }

    [Fact]
    public void EmployerCompanyReviewsController_Class_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(EmployerCompanyReviewsController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/employers/me/company-reviews");
    }

    [Fact]
    public void EmployerCompanyReviewsController_HasNoWriteVerbActions()
    {
        var writeVerbAttributes = typeof(EmployerCompanyReviewsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes())
            .OfType<HttpMethodAttribute>()
            .Where(attribute => attribute is not HttpGetAttribute)
            .ToList();

        writeVerbAttributes.Should().BeEmpty("employers can only read reviews, never write to them");
    }

    [Fact]
    public async Task GetCompanyReviews_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        var query = new GetEmployerCompanyReviewsQuery { Status = CompanyReviewStatus.Published, Page = 1, PageSize = 20 };
        var expectedResult = new PaginatedList<EmployerCompanyReviewDto>(
            new List<EmployerCompanyReviewDto>
            {
                new(
                    Guid.NewGuid(),
                    CompanyReviewStatus.Published,
                    ReviewSentiment.Positive,
                    "Great process",
                    new CompanyReviewRatingsDto(4, 4, 4, 4),
                    "Backend Engineer",
                    DateTime.UtcNow,
                    null)
            },
            1,
            20,
            1);
        _sender
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviews(query, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCompanyReviews_WithNullQuery_DispatchesDefaultQueryAndReturnsOk()
    {
        var expectedResult = new PaginatedList<EmployerCompanyReviewDto>(new List<EmployerCompanyReviewDto>(), 1, 20, 0);
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetEmployerCompanyReviewsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviews(null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(
                It.Is<GetEmployerCompanyReviewsQuery>(q => q.Status == null && q.Sentiment == null && q.Page == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyReviewSummary_DispatchesToSenderAndReturnsOk()
    {
        var expectedResult = new EmployerCompanyReviewSummaryDto(
            new CompanyReviewSummaryDto(
                0, 0, 0, 0m,
                new RatingAreaSummaryDto(null, 0),
                new RatingAreaSummaryDto(null, 0),
                new RatingAreaSummaryDto(null, 0),
                new RatingAreaSummaryDto(null, 0)),
            new Dictionary<string, int>
            {
                ["Pending"] = 0,
                ["Published"] = 0,
                ["Rejected"] = 0,
                ["Hidden"] = 0,
                ["Withdrawn"] = 0
            });
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetEmployerCompanyReviewSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviewSummary(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(sender => sender.Send(It.IsAny<GetEmployerCompanyReviewSummaryQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

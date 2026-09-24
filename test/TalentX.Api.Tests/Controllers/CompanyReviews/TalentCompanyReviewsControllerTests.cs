using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.CompanyReviews;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetMyCompanyReviews;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.CompanyReviews;

public class TalentCompanyReviewsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly TalentCompanyReviewsController _controller;

    public TalentCompanyReviewsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);

        _controller = new TalentCompanyReviewsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
            }
        };
    }

    [Fact]
    public void Controller_RouteIsTheTalentsCompanyReviewsResource()
    {
        var route = typeof(TalentCompanyReviewsController).GetCustomAttribute<RouteAttribute>();

        route.Should().NotBeNull();
        route!.Template.Should().Be("api/v1/talents/me/company-reviews");
    }

    [Theory]
    [InlineData(nameof(TalentCompanyReviewsController.GetMyCompanyReviews))]
    [InlineData(nameof(TalentCompanyReviewsController.SubmitCompanyReview))]
    [InlineData(nameof(TalentCompanyReviewsController.UpdateCompanyReview))]
    [InlineData(nameof(TalentCompanyReviewsController.WithdrawCompanyReview))]
    public void Actions_AreRestrictedToTheTalentRole(string actionName)
    {
        var authorize = typeof(TalentCompanyReviewsController).GetMethod(actionName)!
            .GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public async Task GetMyCompanyReviews_ReturnsOkWithThePage_AndForwardsPaging()
    {
        var page = new PaginatedList<MyCompanyReviewItemDto>(Array.Empty<MyCompanyReviewItemDto>(), 2, 5, 0);
        _sender
            .Setup(s => s.Send(It.IsAny<GetMyCompanyReviewsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _controller.GetMyCompanyReviews(2, 5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
        _sender.Verify(
            s => s.Send(It.Is<GetMyCompanyReviewsQuery>(q => q.Page == 2 && q.PageSize == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitCompanyReview_Returns201WithTheNewReviewId()
    {
        var reviewId = Guid.NewGuid();
        var command = new SubmitCompanyReviewCommand(Guid.NewGuid(), ReviewSentiment.Positive, null, null);
        _sender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewId);

        var result = await _controller.SubmitCompanyReview(command, CancellationToken.None);

        var created = result.Result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(reviewId);
    }

    [Fact]
    public async Task UpdateCompanyReview_RebindsTheRouteIdOverTheBodysReviewId()
    {
        var routeId = Guid.NewGuid();
        var body = new UpdateCompanyReviewCommand(Guid.NewGuid(), ReviewSentiment.Negative, "Updated.", null);

        var result = await _controller.UpdateCompanyReview(routeId, body, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            s => s.Send(It.Is<UpdateCompanyReviewCommand>(c => c.ReviewId == routeId && c.Comment == "Updated."), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WithdrawCompanyReview_SendsTheRouteIdAndReturnsOk()
    {
        var routeId = Guid.NewGuid();

        var result = await _controller.WithdrawCompanyReview(routeId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            s => s.Send(It.Is<WithdrawCompanyReviewCommand>(c => c.ReviewId == routeId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

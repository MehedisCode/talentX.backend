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
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class AdminCompanyReviewsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly AdminCompanyReviewsController _controller;

    public AdminCompanyReviewsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new AdminCompanyReviewsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void AdminCompanyReviewsController_Class_InheritsBaseApiController()
    {
        typeof(AdminCompanyReviewsController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void AdminCompanyReviewsController_Class_RequiresAdminRoleForEveryAction()
    {
        // Declared once on the class, so it covers every action.
        var authorizeAttribute = typeof(AdminCompanyReviewsController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void AdminCompanyReviewsController_Class_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(AdminCompanyReviewsController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/admin/company-reviews");
    }

    [Fact]
    public async Task GetCompanyReviews_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        var query = new GetAdminCompanyReviewsQuery { SearchQuery = "acme", Status = CompanyReviewStatus.Pending, Page = 1, PageSize = 20 };
        var expectedResult = new PaginatedList<AdminCompanyReviewListItemDto>(
            new List<AdminCompanyReviewListItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CompanyName = "Acme Inc",
                    Status = CompanyReviewStatus.Pending,
                    Ratings = new CompanyReviewRatingsDto(null, null, null, null)
                }
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
        var expectedResult = new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto>(), 1, 20, 0);
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAdminCompanyReviewsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviews(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<GetAdminCompanyReviewsQuery>(q => q.SearchQuery == null && q.Status == null && q.Page == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyReviewStats_DispatchesToSenderAndReturnsOk()
    {
        var expectedResult = new AdminCompanyReviewStatsDto { Total = 4, PendingCount = 2 };
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAdminCompanyReviewStatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviewStats(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetCompanyReviewDetails_WithId_DispatchesQueryWithMatchingReviewIdAndReturnsOk()
    {
        var reviewId = Guid.NewGuid();
        var expectedResult = new AdminCompanyReviewDetailsDto
        {
            Id = reviewId,
            CompanyName = "Acme Inc",
            Ratings = new CompanyReviewRatingsDto(null, null, null, null)
        };
        _sender
            .Setup(sender => sender.Send(It.Is<GetAdminCompanyReviewDetailsQuery>(q => q.ReviewId == reviewId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetCompanyReviewDetails(reviewId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task ApproveCompanyReview_WithId_DispatchesCommandWithMatchingReviewIdAndReturnsOk()
    {
        var reviewId = Guid.NewGuid();
        var expectedResult = new StatusUpdateResponseDto("Review approved.");
        _sender
            .Setup(sender => sender.Send(It.Is<ApproveCompanyReviewCommand>(c => c.ReviewId == reviewId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.ApproveCompanyReview(reviewId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task RejectCompanyReview_UsesTheRouteIdEvenWhenTheBodyCarriesADifferentReviewId()
    {
        var routeReviewId = Guid.NewGuid();
        var bodyReviewId = Guid.NewGuid();
        var command = new RejectCompanyReviewCommand(bodyReviewId, ReviewModerationReason.Spam, "This looks like a spam submission.");
        var expectedResult = new StatusUpdateResponseDto("Review rejected.");
        _sender
            .Setup(sender => sender.Send(It.IsAny<RejectCompanyReviewCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.RejectCompanyReview(routeReviewId, command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(
                It.Is<RejectCompanyReviewCommand>(c =>
                    c.ReviewId == routeReviewId &&
                    c.Reason == ReviewModerationReason.Spam &&
                    c.Details == "This looks like a spam submission."),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sender.Verify(
            sender => sender.Send(It.Is<RejectCompanyReviewCommand>(c => c.ReviewId == bodyReviewId), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HideCompanyReview_UsesTheRouteIdEvenWhenTheBodyCarriesADifferentReviewId()
    {
        var routeReviewId = Guid.NewGuid();
        var bodyReviewId = Guid.NewGuid();
        var command = new HideCompanyReviewCommand(bodyReviewId, ReviewModerationReason.PersonalInformation, "Contains the employer's home address.");
        var expectedResult = new StatusUpdateResponseDto("Review hidden.");
        _sender
            .Setup(sender => sender.Send(It.IsAny<HideCompanyReviewCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.HideCompanyReview(routeReviewId, command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(
                It.Is<HideCompanyReviewCommand>(c =>
                    c.ReviewId == routeReviewId &&
                    c.Reason == ReviewModerationReason.PersonalInformation),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sender.Verify(
            sender => sender.Send(It.Is<HideCompanyReviewCommand>(c => c.ReviewId == bodyReviewId), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnhideCompanyReview_WithId_DispatchesCommandWithMatchingReviewIdAndReturnsOk()
    {
        var reviewId = Guid.NewGuid();
        var expectedResult = new StatusUpdateResponseDto("Review unhidden.");
        _sender
            .Setup(sender => sender.Send(It.Is<UnhideCompanyReviewCommand>(c => c.ReviewId == reviewId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.UnhideCompanyReview(reviewId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }
}

using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Employers;
using TalentX.Application.Features.Invitations.Commands.SendJobInvitation;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Features.Invitations.Queries.GetJobInvitations;
using TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Features.TalentMatching.Queries.GetCandidateSummary;
using TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Employers;

public class EmployerTalentMatchingControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly EmployerTalentMatchingController _controller;

    public EmployerTalentMatchingControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new EmployerTalentMatchingController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public async Task RunMatching_SendsCommandAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var response = new RunJobTalentMatchingResponse(jobId, 1, new List<MatchedCandidateDto>());

        _sender.Setup(s => s.Send(It.Is<RunJobTalentMatchingCommand>(c => c.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.RunMatching(jobId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetMatchedCandidates_SendsQueryAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var candidates = new List<MatchedCandidateDto>
        {
            new() { TalentUserId = "talent-1", Score = 88.0m, Name = "Alice" }
        };

        _sender.Setup(s => s.Send(It.Is<GetMatchedCandidatesQuery>(q => q.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var result = await _controller.GetMatchedCandidates(jobId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(candidates);
    }

    [Fact]
    public async Task SendInvitation_SendsCommandAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var request = new SendInvitationRequestDto("talent-123");
        var response = new SendJobInvitationResponse(Guid.NewGuid(), jobId, "talent-123", DateTime.UtcNow, true);

        _sender.Setup(s => s.Send(It.Is<SendJobInvitationCommand>(c => c.JobId == jobId && c.TalentUserId == "talent-123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.SendInvitation(jobId, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetJobInvitations_SendsQueryAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var invitations = new List<JobInvitationDto>
        {
            new() { Id = Guid.NewGuid(), JobId = jobId, TalentUserId = "talent-1", IsActive = true }
        };

        _sender.Setup(s => s.Send(It.Is<GetJobInvitationsQuery>(q => q.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

        var result = await _controller.GetJobInvitations(jobId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(invitations);
    }

    [Fact]
    public async Task GetCandidateSummary_SendsQueryAndReturnsOk()
    {
        var candidateId = "candidate-123";
        var summary = new CandidateProfileSummary { UserId = candidateId, Name = "Alice" };

        _sender.Setup(s => s.Send(It.Is<GetCandidateSummaryQuery>(q => q.Identifier == candidateId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.GetCandidateSummary(candidateId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(summary);
    }
}

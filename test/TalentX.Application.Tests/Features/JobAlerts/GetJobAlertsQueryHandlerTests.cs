using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.JobAlerts.Queries.GetJobAlerts;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class GetJobAlertsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetJobAlertsQueryHandler _handler;

    public GetJobAlertsQueryHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);

        _handler = new GetJobAlertsQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task Handle_WhenAlertsExist_ReturnsMappedAlertList()
    {
        var alert = JobAlert.Create(
            "talent-user-1",
            "AI Roles",
            "Machine Learning",
            "Remote",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            WorkArrangement.FullRemote);

        var alerts = new List<JobAlert> { alert };

        var expectedDtos = new List<JobAlertDto>
        {
            new(
                alert.Id,
                alert.Name,
                alert.Title,
                alert.Location,
                alert.EmploymentType,
                alert.ExperienceLevel,
                alert.WorkArrangement,
                alert.IsEnabled,
                alert.CreatedAt)
        };

        _jobAlertRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        _mapper
            .Setup(mapper => mapper.Map<List<JobAlertDto>>(alerts))
            .Returns(expectedDtos);

        var result = await _handler.Handle(new GetJobAlertsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("AI Roles");
        result[0].Title.Should().Be("Machine Learning");
        result[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNoAlerts_ReturnsEmptyList()
    {
        _jobAlertRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobAlert>());

        _mapper
            .Setup(mapper => mapper.Map<List<JobAlertDto>>(It.IsAny<List<JobAlert>>()))
            .Returns(new List<JobAlertDto>());

        var result = await _handler.Handle(new GetJobAlertsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var action = () => _handler.Handle(new GetJobAlertsQuery(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}

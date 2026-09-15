using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using AutoMapper;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetJobDetailsQueryTests
{
    private readonly IMapper _mapper;

    public GetJobDetailsQueryTests()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<JobDetailsDto>(It.IsAny<Job>()))
            .Returns((Job source) => new JobDetailsDto { Id = source.Id, Title = source.Title });
        _mapper = mapperMock.Object;
    }

    [Fact]
    public async Task Handle_WhenJobExpiresToday_StatusIsNotExpired()
    {
        // A job expiring today should still read as active through the entire
        // day, not flip to "Expired" the instant the expiry date begins.
        var job = Job.Create(
            "employer-1",
            "Test Role",
            "Test Co",
            "Test description for the role.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.Date
        );
        job.MarkAsPublished();

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper);
        var query = new GetJobDetailsQuery(job.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Status.Should().Be("Published");
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenRepositoryReturnsNull()
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper);
        var query = new GetJobDetailsQuery(Guid.NewGuid());

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenJobExists()
    {
        var job = Job.Create(
            "employer-1",
            "Test Role",
            "Test Co",
            "Test description for the role.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddMonths(1)
        );
        job.MarkAsPublished();

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper);
        var query = new GetJobDetailsQuery(job.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Role");
    }
}

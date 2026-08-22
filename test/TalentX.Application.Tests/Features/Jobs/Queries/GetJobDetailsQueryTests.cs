using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries;
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
        var job = new Job
        {
            Title = "Test Role",
            CompanyName = "Test Co",
            Description = "Test description for the role.",
            EmploymentType = EmploymentType.FullTime,
            ExperienceLevel = ExperienceLevel.MidLevel,
            Location = "Dhaka",
            WorkMode = WorkMode.Hybrid,
            RequiredSkills = new[] { "C#" }.ToList(),
            Vacancies = 1,
            ExpireAt = DateTime.UtcNow.AddMonths(1),
            Status = JobStatus.Published
        };

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

using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using AutoMapper;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetPublishedJobsQueryTests
{
    private readonly IMapper _mapper;

    public GetPublishedJobsQueryTests()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<JobListItemDto>>(It.IsAny<List<Job>>()))
            .Returns((List<Job> source) => source.Select(s => new JobListItemDto { Id = s.Id, Title = s.Title }).ToList());
        _mapper = mapperMock.Object;
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedList()
    {
        var jobs = new List<Job>
        {
            new Job { Title = "Job 1", Status = JobStatus.Published },
            new Job { Title = "Job 2", Status = JobStatus.Published }
        };

        var paginatedJobs = new PaginatedList<Job>(jobs, 1, 20, 2);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetPublishedJobsQueryHandler(unitOfWork.Object, _mapper);
        var query = new GetPublishedJobsQuery(1, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.Items[0].Title.Should().Be("Job 1");
        result.TotalCount.Should().Be(2);
    }
}

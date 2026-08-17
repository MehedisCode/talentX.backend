using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Queries;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Tests.Jobs;

public class GetPublishedJobsQueryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(5, 5)]
    public async Task Handle_ClampsInvalidPageNumber(int requestedPage, int expectedPage)
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(expectedPage, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Job>(), 0));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetPublishedJobsQueryHandler(unitOfWork.Object);
        var query = new GetPublishedJobsQuery(requestedPage, 20);

        await handler.Handle(query, CancellationToken.None);

        jobRepo.Verify(r => r.GetPublishedJobsAsync(expectedPage, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

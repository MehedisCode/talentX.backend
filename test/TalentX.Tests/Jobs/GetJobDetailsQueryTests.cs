using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Queries;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Tests.Jobs;

public class GetJobDetailsQueryTests
{
    [Fact]
    public async Task Handle_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object);
        var query = new GetJobDetailsQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenJobExists()
    {
        var job = new Job(Guid.NewGuid(), "Test Co", "Test Role");
        job.UpdateDetails(
            title: "Test Role",
            companyName: "Test Co",
            roleType: null, experienceLevel: null, locationPref: null,
            city: null, country: null, minOverlapHours: null,
            description: null, responsibilities: null,
            skills: new[] { "C#" }, expiresAt: null);
        job.Publish();
        
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object);
        var query = new GetJobDetailsQuery(job.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test Role", result!.Title);
    }
}

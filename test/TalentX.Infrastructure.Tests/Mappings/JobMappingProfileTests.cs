using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Mappings;
using Xunit;

namespace TalentX.Infrastructure.Tests.Mappings;

public sealed class JobMappingProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<JobMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    // A published job with a deadline far enough ahead to be reopened.
    private static Job PublishedJob()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    // Closes and reopens the job a moment later, so its latest publish is after its first.
    private static Job RepostedJob()
    {
        var job = PublishedJob();
        Thread.Sleep(5);
        job.MarkAsClosed();
        job.Reopen();
        return job;
    }

    [Fact]
    public void JobListItem_ForAJobPublishedOnce_IsNotReposted()
    {
        var job = PublishedJob();

        var dto = _mapper.Map<JobListItemDto>(job);

        dto.IsReposted.Should().BeFalse();
        dto.CreatedAt.Should().Be(job.PublishedAt!.Value);
    }

    [Fact]
    public void JobListItem_ForARelistedJob_IsRepostedAndKeepsTheLatestPublishDate()
    {
        var job = RepostedJob();

        var dto = _mapper.Map<JobListItemDto>(job);

        dto.IsReposted.Should().BeTrue();
        dto.CreatedAt.Should().Be(job.PublishedAt!.Value);
    }

    [Fact]
    public void JobDetails_ForARelistedJob_IsReposted()
    {
        var dto = _mapper.Map<JobDetailsDto>(RepostedJob());

        dto.IsReposted.Should().BeTrue();
    }
}

using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using AutoMapper;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetJobDetailsQueryTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();

    public GetJobDetailsQueryTests()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<JobDetailsDto>(It.IsAny<Job>()))
            .Returns((Job source) => new JobDetailsDto
            {
                Id = source.Id,
                EmployerId = source.EmployerId,
                Title = source.Title,
                CompanyName = source.CompanyName,
            });
        _mapper = mapperMock.Object;
    }

    private static Mock<IUnitOfWork> BuildUnitOfWork(Job job, Company? company)
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByEmployerIdAsync(job.EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);
        unitOfWork.Setup(u => u.Companies).Returns(companyRepo.Object);
        return unitOfWork;
    }

    private static Job CreatePublishedJob(string employerId = "employer-1", string companyName = "Test Co")
    {
        var job = Job.Create(
            employerId,
            "Test Role",
            companyName,
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
        return job;
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

        var unitOfWork = BuildUnitOfWork(job, null);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);
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
        unitOfWork.Setup(u => u.Companies).Returns(new Mock<ICompanyRepository>().Object);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);
        var query = new GetJobDetailsQuery(Guid.NewGuid());

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenJobExists()
    {
        var job = CreatePublishedJob();
        var unitOfWork = BuildUnitOfWork(job, null);

        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);
        var query = new GetJobDetailsQuery(job.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Role");
    }

    [Fact]
    public async Task Handle_UsesCompanyName_WhenCompanyExists()
    {
        var job = CreatePublishedJob(companyName: "Stale Snapshot Co");
        var company = new Company(job.EmployerId, "Canonical Company Name");

        var unitOfWork = BuildUnitOfWork(job, company);
        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);

        var result = await handler.Handle(new GetJobDetailsQuery(job.Id), CancellationToken.None);

        result.CompanyName.Should().Be("Canonical Company Name");
        result.CompanyId.Should().Be(company.Id);
        result.LogoUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsLogoUrl_WhenCompanyHasLogo()
    {
        var job = CreatePublishedJob();
        var company = new Company(job.EmployerId, "Canonical Company Name");
        company.UpdateLogo("logos/acme.png");

        _presignedUrlService.Setup(s => s.CreateDownloadUrlAsync(
                "logos/acme.png", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.example/logos/acme.png");

        var unitOfWork = BuildUnitOfWork(job, company);
        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);

        var result = await handler.Handle(new GetJobDetailsQuery(job.Id), CancellationToken.None);

        result.LogoUrl.Should().Be("https://cdn.example/logos/acme.png");
    }

    [Fact]
    public async Task Handle_FallsBackToJobCompanyName_WhenNoCompanyProfile()
    {
        var job = CreatePublishedJob(companyName: "Snapshot Co");
        var unitOfWork = BuildUnitOfWork(job, null);
        var handler = new GetJobDetailsQueryHandler(unitOfWork.Object, _mapper, _presignedUrlService.Object);

        var result = await handler.Handle(new GetJobDetailsQuery(job.Id), CancellationToken.None);

        result.CompanyName.Should().Be("Snapshot Co");
        result.CompanyId.Should().BeNull();
        result.LogoUrl.Should().BeNull();
    }
}

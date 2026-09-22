using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using AutoMapper;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetPublishedJobsQueryTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();

    public GetPublishedJobsQueryTests()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<JobListItemDto>>(It.IsAny<List<Job>>()))
            .Returns((List<Job> source) => source.Select(s => new JobListItemDto
            {
                Id = s.Id,
                EmployerId = s.EmployerId,
                Title = s.Title,
                CompanyName = s.CompanyName,
                Location = s.Location,
                EmploymentType = s.EmploymentType.ToString(),
                ExperienceLevel = s.ExperienceLevel.ToString(),
                WorkArrangement = s.WorkArrangement.ToString(),
                CreatedAt = s.CreatedAt,
                ExpireAt = s.ExpireAt,
            }).ToList());
        _mapper = mapperMock.Object;
    }

    private static Job MakeJob(string title = "Job 1", string company = "Co 1",
        EmploymentType empType = EmploymentType.FullTime,
        ExperienceLevel expLevel = ExperienceLevel.MidLevel,
        WorkArrangement arrangement = WorkArrangement.RemoteButAccessible,
        string location = "Dhaka",
        string employerId = "employer-1")
    {
        var job = Job.Create(employerId, title, company, "Desc", empType, expLevel, location,
            arrangement, new List<string>(), 1, DateTime.UtcNow.AddDays(10));
        job.MarkAsPublished();
        return job;
    }

    private static Mock<IUnitOfWork> BuildUnitOfWork(
        PaginatedList<Job> paginatedJobs,
        Mock<IJobRepository> jobRepo,
        IReadOnlyList<Company>? companies = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);

        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByEmployerIdsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies ?? Array.Empty<Company>());
        unitOfWork.Setup(u => u.Companies).Returns(companyRepo.Object);

        return unitOfWork;
    }

    private GetPublishedJobsQueryHandler CreateHandler(Mock<IUnitOfWork> unitOfWork)
        => new(unitOfWork.Object, _mapper, _presignedUrlService.Object);

    [Fact]
    public async Task Handle_ReturnsPaginatedList()
    {
        var job1 = Job.Create("employer-1", "Job 1", "Co 1", "Desc 1", EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.FullRemote, new List<string>(), 1, DateTime.UtcNow.AddDays(10));
        job1.MarkAsPublished();
        var job2 = Job.Create("employer-1", "Job 2", "Co 2", "Desc 2", EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.FullRemote, new List<string>(), 1, DateTime.UtcNow.AddDays(10));
        job2.MarkAsPublished();

        var jobs = new List<Job> { job1, job2 };
        var paginatedJobs = new PaginatedList<Job>(jobs, 1, 20, 2);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                1, 20, It.IsAny<CancellationToken>(),
                null, null, null, null, null))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo);

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(new GetPublishedJobsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ForwardsKeywordFilterToRepository()
    {
        var job = MakeJob("React Developer");
        var paginatedJobs = new PaginatedList<Job>(new List<Job> { job }, 1, 20, 1);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                1, 20, It.IsAny<CancellationToken>(),
                "react", null, null, null, null))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo);

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(
            new GetPublishedJobsQuery(Keyword: "react", Page: 1, PageSize: 20),
            CancellationToken.None);

        result.Items.Count.Should().Be(1);
        jobRepo.Verify(r => r.GetPublishedJobsAsync(
            1, 20, It.IsAny<CancellationToken>(), "react", null, null, null, null), Times.Once);
    }

    [Fact]
    public async Task Handle_ForwardsEmploymentTypeFilterToRepository()
    {
        var job = MakeJob(empType: EmploymentType.Contract);
        var paginatedJobs = new PaginatedList<Job>(new List<Job> { job }, 1, 20, 1);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                1, 20, It.IsAny<CancellationToken>(),
                null, null, EmploymentType.Contract, null, null))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo);

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(
            new GetPublishedJobsQuery(EmploymentType: EmploymentType.Contract, Page: 1, PageSize: 20),
            CancellationToken.None);

        result.Items.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ForwardsAllFiltersToRepository()
    {
        var paginatedJobs = new PaginatedList<Job>(new List<Job>(), 1, 10, 0);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                1, 10, It.IsAny<CancellationToken>(),
                "engineer", "Dhaka",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, WorkArrangement.Hybrid))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo);

        var handler = CreateHandler(unitOfWork);
        await handler.Handle(new GetPublishedJobsQuery(
            Keyword: "engineer",
            Location: "Dhaka",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.SeniorLevel,
            WorkArrangement: WorkArrangement.Hybrid,
            Page: 1,
            PageSize: 10), CancellationToken.None);

        jobRepo.Verify(r => r.GetPublishedJobsAsync(
            1, 10, It.IsAny<CancellationToken>(),
            "engineer", "Dhaka",
            EmploymentType.FullTime, ExperienceLevel.SeniorLevel, WorkArrangement.Hybrid), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoJobsMatch()
    {
        var paginatedJobs = new PaginatedList<Job>(new List<Job>(), 1, 20, 0);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<EmploymentType?>(), It.IsAny<ExperienceLevel?>(), It.IsAny<WorkArrangement?>()))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo);

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(
            new GetPublishedJobsQuery(Keyword: "zxqwerty999noresults"),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UsesCompanyNameAndLogo_WhenCompanyExists()
    {
        var job = MakeJob(company: "Stale Snapshot Co");
        var paginatedJobs = new PaginatedList<Job>(new List<Job> { job }, 1, 20, 1);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<EmploymentType?>(), It.IsAny<ExperienceLevel?>(), It.IsAny<WorkArrangement?>()))
            .ReturnsAsync(paginatedJobs);

        var company = new Company(job.EmployerId, "Canonical Company");
        company.UpdateLogo("logos/acme.png");

        _presignedUrlService.Setup(s => s.CreateDownloadUrlAsync(
                "logos/acme.png", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.example/logos/acme.png");

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo, new List<Company> { company });

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(new GetPublishedJobsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].CompanyName.Should().Be("Canonical Company");
        result.Items[0].CompanyId.Should().Be(company.Id);
        result.Items[0].LogoUrl.Should().Be("https://cdn.example/logos/acme.png");
    }

    [Fact]
    public async Task Handle_FallsBackToJobCompanyName_WhenNoCompanyProfile()
    {
        var job = MakeJob(company: "Snapshot Co");
        var paginatedJobs = new PaginatedList<Job>(new List<Job> { job }, 1, 20, 1);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<EmploymentType?>(), It.IsAny<ExperienceLevel?>(), It.IsAny<WorkArrangement?>()))
            .ReturnsAsync(paginatedJobs);

        var unitOfWork = BuildUnitOfWork(paginatedJobs, jobRepo, Array.Empty<Company>());

        var handler = CreateHandler(unitOfWork);
        var result = await handler.Handle(new GetPublishedJobsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items[0].CompanyName.Should().Be("Snapshot Co");
        result.Items[0].CompanyId.Should().BeNull();
        result.Items[0].LogoUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FetchesCompaniesOnce_ForDistinctEmployers()
    {
        var job1 = MakeJob(employerId: "employer-1");
        var job2 = MakeJob(employerId: "employer-1");
        var job3 = MakeJob(employerId: "employer-2");
        var paginatedJobs = new PaginatedList<Job>(new List<Job> { job1, job2, job3 }, 1, 20, 3);

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetPublishedJobsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<EmploymentType?>(), It.IsAny<ExperienceLevel?>(), It.IsAny<WorkArrangement?>()))
            .ReturnsAsync(paginatedJobs);

        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByEmployerIdsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Company>());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Jobs).Returns(jobRepo.Object);
        unitOfWork.Setup(u => u.Companies).Returns(companyRepo.Object);

        var handler = CreateHandler(unitOfWork);
        await handler.Handle(new GetPublishedJobsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        companyRepo.Verify(r => r.GetByEmployerIdsAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 2
                && ids.Contains("employer-1")
                && ids.Contains("employer-2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

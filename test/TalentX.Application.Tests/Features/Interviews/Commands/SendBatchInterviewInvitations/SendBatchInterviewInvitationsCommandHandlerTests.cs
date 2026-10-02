using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews.Commands.SendBatchInterviewInvitations;

public class SendBatchInterviewInvitationsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepository = new();
    private readonly Mock<IEmailDispatchLogRepository> _emailDispatchLogRepository = new();
    private readonly Mock<IJobApplicationStatusHistoryRepository> _historyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly SendBatchInterviewInvitationsCommandHandler _handler;

    public SendBatchInterviewInvitationsCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_interviewInvitationRepository.Object);
        _unitOfWork.SetupGet(u => u.EmailDispatchLogs).Returns(_emailDispatchLogRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplicationStatusHistories).Returns(_historyRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");

        _handler = new SendBatchInterviewInvitationsCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _emailService.Object);
    }

    private static Job CreateJob(string employerId)
    {
        return Job.Create(
            employerId,
            "Product Designer",
            "Design Studio",
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "Figma" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static JobApplication CreateApplication(Guid jobId, string talentUserId)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Alex Smith",
            "alex@example.com",
            "resumes/alex.pdf");
    }

    private void SetUpNoExistingInvite(Guid applicationId)
    {
        _interviewInvitationRepository
            .Setup(r => r.GetByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewInvitation?)null);
    }

    [Fact]
    public async Task Handle_WhenInviteIsSent_UpdatesApplicationStatusToInvited()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-1");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        SetUpNoExistingInvite(application.Id);

        var command = new SendBatchInterviewInvitationsCommand(
            job.Id,
            new List<Guid> { application.Id },
            InterviewType.OnCall,
            LocationOrLink: "https://meet.example.com/room");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(1);
        result.InvitedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.Results.Should().ContainSingle(r => r.Success && r.ApplicationId == application.Id);
        application.Status.Should().Be(ApplicationStatus.Invited);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Rejected)]
    public async Task Handle_WhenApplicationIsAlreadyResolved_SkipsAndLeavesStatusUnchanged(ApplicationStatus existingStatus)
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-1");
        application.UpdateStatus(existingStatus);

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        SetUpNoExistingInvite(application.Id);

        var command = new SendBatchInterviewInvitationsCommand(
            job.Id,
            new List<Guid> { application.Id },
            InterviewType.OnCall,
            LocationOrLink: "https://meet.example.com/room");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(1);
        result.InvitedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Results.Should().ContainSingle(r => !r.Success && r.SkipReason != null && r.SkipReason.Contains(existingStatus.ToString()));
        application.Status.Should().Be(existingStatus);
        _interviewInvitationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewInvitation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPendingInviteExists_SkipsCandidateAndReportsReason()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-1");
        var pendingInvite = new InterviewInvitation(
            job.Id,
            application.Id,
            "employer-1",
            "talent-1",
            InterviewType.OnCall,
            locationOrLink: "https://meet.example.com/room");

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _interviewInvitationRepository
            .Setup(r => r.GetByApplicationIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingInvite);

        var command = new SendBatchInterviewInvitationsCommand(
            job.Id,
            new List<Guid> { application.Id },
            InterviewType.OnCall,
            LocationOrLink: "https://meet.example.com/room");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(1);
        result.InvitedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Results.Should().ContainSingle(r => !r.Success && r.SkipReason != null && r.SkipReason.Contains("pending interview invitation"));
        _interviewInvitationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewInvitation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

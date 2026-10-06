using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.GeneratePreparation;

public class GeneratePreparationCommandHandlerTests
{
    private const string TalentUserId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewInvitationRepository> _invitationRepository = new();
    private readonly Mock<IInterviewPreparationRepository> _preparationRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IInterviewPreparationAiService> _aiService = new();
    private readonly Mock<IInterviewPreparationConfigProvider> _configProvider = new();
    private readonly Mock<ILogger<GeneratePreparationCommandHandler>> _logger = new();
    private readonly GeneratePreparationCommandHandler _handler;

    public GeneratePreparationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_invitationRepository.Object);
        _unitOfWork.SetupGet(u => u.InterviewPreparations).Returns(_preparationRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentUserId);
        _configProvider.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(InterviewPreparationConfig.CreateDefault());

        _handler = new GeneratePreparationCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _aiService.Object,
            _configProvider.Object,
            _logger.Object);
    }

    private static InterviewInvitation CreateAcceptedInvitation(Guid jobId, string talentUserId)
    {
        var invitation = new InterviewInvitation(
            jobId, Guid.NewGuid(), "employer-1", talentUserId, InterviewType.OnCall);
        invitation.Accept(selectedTimeslotId: null, scheduledAt: DateTime.UtcNow.AddHours(2));
        return invitation;
    }

    private static Job CreateJob()
    {
        return Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme",
            "Build scalable systems",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#", ".NET" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static Talent CreateTalent()
    {
        var talent = new Talent(TalentUserId, "Ada Lovelace", "ada@example.com");
        talent.UpdateProfile(
            "Ada Lovelace",
            "Distributed systems engineer",
            "I build reliable backends",
            "London",
            null,
            null,
            null,
            6,
            null,
            null);
        return talent;
    }

    private static InterviewPreparationResult AiResult()
    {
        return new InterviewPreparationResult
        {
            TechnicalQuestions = """[{"question":"What is a closure?","category":"Technical","tip":"Scope"}]""",
            BehavioralQuestions = "[]",
            ExperienceBasedQuestions = "[]",
            RoleSpecificQuestions = "[]",
            KeyTopicsToPrepare = """["Idempotency"]""",
            SuggestedRevisionAreas = """["SQL tuning"]""",
        };
    }

    private void SetUpAcceptedInvitationFlow(
        InterviewInvitation invitation,
        InterviewPreparationEntity? existingPreparation = null,
        int todayCount = 0)
    {
        var job = CreateJob();
        var talent = CreateTalent();

        _invitationRepository
            .Setup(r => r.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _preparationRepository
            .Setup(r => r.GetByInvitationIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreparation);
        _preparationRepository
            .Setup(r => r.CountInitialGenerationsTodayByTalentAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(todayCount);
        _jobRepository
            .Setup(r => r.GetByIdAsync(invitation.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>
            {
                new(TalentUserId, "Globex", "Backend Engineer", "2020", null, true),
            });
        _educationRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>
            {
                new(TalentUserId, "MIT", "BSc Computer Science", "2014", "2018"),
            });
        _aiService
            .Setup(s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiResult());
    }

    [Fact]
    public async Task Handle_WhenEverythingIsValid_CreatesPreparationAndReturnsMappedDto()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation);

        var result = await _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        result.InterviewInvitationId.Should().Be(invitation.Id);
        result.JobTitle.Should().Be("Backend Engineer");
        result.CompanyName.Should().Be("Acme");
        result.InterviewType.Should().Be("OnCall");
        result.Status.Should().Be("Generated");
        result.GenerationCount.Should().Be(1);
        result.IsAccessible.Should().BeTrue();
        result.AdditionalQuestions.Should().BeEmpty();
        result.TechnicalQuestions.Should().HaveCount(1);
        result.TechnicalQuestions[0].Question.Should().Be("What is a closure?");
        result.KeyTopicsToPrepare.Should().Equal("Idempotency");
        result.SuggestedRevisionAreas.Should().Equal("SQL tuning");

        _preparationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewPreparationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _preparationRepository.Verify(
            r => r.AddGenerationAsync(
                It.Is<InterviewPreparationGeneration>(g =>
                    g.Kind == InterviewPreparationGenerationKind.Initial
                    && g.TalentUserId == TalentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BuildsAiContextFromJobTalentAndInvitation()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation);

        await _handler.Handle(new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        _aiService.Verify(
            s => s.GeneratePreparationAsync(
                It.Is<InterviewPreparationContext>(c =>
                    c.JobTitle == "Backend Engineer" &&
                    c.CompanyName == "Acme" &&
                    c.TalentName == "Ada Lovelace" &&
                    c.InterviewType == "OnCall" &&
                    c.RequiredSkills.Contains("C#") &&
                    c.WorkExperiences.Count == 1 &&
                    c.WorkExperiences[0].CompanyName == "Globex" &&
                    c.Educations.Count == 1 &&
                    c.Educations[0].Institution == "MIT"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_ThrowsNotFoundException()
    {
        var invitationId = Guid.NewGuid();
        _invitationRepository
            .Setup(r => r.GetByIdAsync(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewInvitation?)null);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Interview invitation not found.");
    }

    [Fact]
    public async Task Handle_WhenInvitationBelongsToAnotherTalent_ThrowsForbiddenException()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), "talent-2");

        _invitationRepository
            .Setup(r => r.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this interview.");
        _aiService.Verify(
            s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvitationIsNotAccepted_ThrowsBadRequestException()
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(), Guid.NewGuid(), "employer-1", TalentUserId, InterviewType.OnSite);

        _invitationRepository
            .Setup(r => r.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Interview preparation is only available for accepted interviews.");
    }

    [Fact]
    public async Task Handle_WhenPreparationAlreadyExists_ThrowsConflictException()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        var existing = InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: invitation.Id,
                TalentUserId: TalentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: null,
                TechnicalQuestions: "[]",
                BehavioralQuestions: "[]",
                ExperienceBasedQuestions: "[]",
                RoleSpecificQuestions: "[]",
                KeyTopicsToPrepare: "[]",
                SuggestedRevisionAreas: "[]"));
        SetUpAcceptedInvitationFlow(invitation, existing);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Preparation already exists. Use regenerate instead.");
        _preparationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewPreparationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDailyLimitIsReached_ThrowsTooManyRequestsException()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation, todayCount: 5);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<TooManyRequestsException>()
            .WithMessage("Daily interview preparation limit reached (5/day).");
        _aiService.Verify(
            s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation);
        _jobRepository
            .Setup(r => r.GetByIdAsync(invitation.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Job not found.");
    }

    [Fact]
    public async Task Handle_WhenTalentProfileDoesNotExist_ThrowsNotFoundException()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation);
        _talentRepository
            .Setup(r => r.GetByUserIdAsync(TalentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Talent profile not found.");
    }

    [Fact]
    public async Task Handle_WhenAiServiceFails_ThrowsBadGatewayAndDoesNotPersist()
    {
        var invitation = CreateAcceptedInvitation(Guid.NewGuid(), TalentUserId);
        SetUpAcceptedInvitationFlow(invitation);
        _aiService
            .Setup(s => s.GeneratePreparationAsync(
                It.IsAny<InterviewPreparationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gemini API error"));

        var act = () => _handler.Handle(
            new GeneratePreparationCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("AI service is temporarily unavailable.");
        _preparationRepository.Verify(
            r => r.AddAsync(It.IsAny<InterviewPreparationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.SubmitCompanyReview;

public class SubmitCompanyReviewCommandHandlerTests
{
    private const string TalentId = "talent-1";
    private const string EmployerId = "employer-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _applicationRepository = new();
    private readonly Mock<IInterviewInvitationRepository> _invitationRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ICompanyReviewSettings> _settings = new();
    private readonly SubmitCompanyReviewCommandHandler _handler;

    private readonly Job _job = CreateJob();
    private readonly JobApplication _application;
    private readonly Company _company = new(EmployerId, "Acme Corp");
    private CompanyReview? _added;

    public SubmitCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_applicationRepository.Object);
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_invitationRepository.Object);
        _unitOfWork.SetupGet(u => u.Companies).Returns(_companyRepository.Object);
        _unitOfWork.SetupGet(u => u.CompanyReviews).Returns(_reviewRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentId);
        _settings.SetupGet(s => s.AssessmentReviewDelayDays).Returns(1);

        _application = new JobApplication(_job.Id, TalentId, "Alex Smith", "alex@example.com", "resumes/alex.pdf");

        _applicationRepository
            .Setup(r => r.GetByIdAsync(_application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_application);
        _reviewRepository
            .Setup(r => r.GetByJobApplicationIdAsync(_application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyReview?)null);
        _reviewRepository
            .Setup(r => r.AddAsync(It.IsAny<CompanyReview>(), It.IsAny<CancellationToken>()))
            .Callback<CompanyReview, CancellationToken>((review, _) => _added = review)
            .Returns(Task.CompletedTask);
        _jobRepository
            .Setup(r => r.GetByIdIncludingDeletedAsync(_job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_job);
        _companyRepository
            .Setup(r => r.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_company);

        SetAcceptedInvitations(EndedCall());

        _handler = new SubmitCompanyReviewCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _settings.Object);
    }

    private static Job CreateJob() => Job.Create(
        EmployerId,
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

    private static InterviewInvitation SlotInvitation(DateTime slotEnd)
    {
        var invitation = new InterviewInvitation(Guid.NewGuid(), Guid.NewGuid(), EmployerId, TalentId, InterviewType.OnCall);
        invitation.AddTimeslot(slotEnd.AddHours(-1), slotEnd);
        invitation.Accept(invitation.Timeslots[0].Id, invitation.Timeslots[0].StartTime);
        return invitation;
    }

    private static InterviewInvitation EndedCall() => SlotInvitation(DateTime.UtcNow.AddDays(-1));

    private static InterviewInvitation UpcomingCall() => SlotInvitation(DateTime.UtcNow.AddDays(3));

    private static InterviewInvitation Assessment(DateTime deadline)
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(), Guid.NewGuid(), EmployerId, TalentId, InterviewType.OnlineAssessment, assessmentDeadline: deadline);
        invitation.Accept(null, null);
        return invitation;
    }

    private void SetAcceptedInvitations(params InterviewInvitation[] invitations) =>
        _invitationRepository
            .Setup(r => r.GetAcceptedByApplicationIdAsync(_application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

    private SubmitCompanyReviewCommand Command(
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        string? comment = "Great process.",
        CompanyReviewRatingsDto? ratings = null) =>
        new(_application.Id, sentiment, comment, ratings);

    private void VerifyNothingSaved()
    {
        _reviewRepository.Verify(r => r.AddAsync(It.IsAny<CompanyReview>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Happy path ----

    [Fact]
    public async Task Handle_WhenEligible_CreatesPendingReviewOnTheCompany()
    {
        var ratings = new CompanyReviewRatingsDto(5, 4, null, 2);

        var id = await _handler.Handle(Command(ReviewSentiment.Negative, "Slow replies.", ratings), CancellationToken.None);

        _added.Should().NotBeNull();
        id.Should().Be(_added!.Id);
        _added.Status.Should().Be(CompanyReviewStatus.Pending);
        _added.CompanyId.Should().Be(_company.Id);
        _added.JobId.Should().Be(_job.Id);
        _added.JobApplicationId.Should().Be(_application.Id);
        _added.TalentUserId.Should().Be(TalentId);
        _added.JobTitle.Should().Be("Product Designer");
        _added.Sentiment.Should().Be(ReviewSentiment.Negative);
        _added.Comment.Should().Be("Slow replies.");
        _added.RecruitmentProcessRating.Should().Be(5);
        _added.CommunicationRating.Should().Be(4);
        _added.InterviewExperienceRating.Should().BeNull();
        _added.ProfessionalismRating.Should().Be(2);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_BlankComment_IsStoredAsNull(string? comment)
    {
        await _handler.Handle(Command(comment: comment), CancellationToken.None);

        _added!.Comment.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CommentIsTrimmed()
    {
        await _handler.Handle(Command(comment: "  Friendly team.  "), CancellationToken.None);

        _added!.Comment.Should().Be("Friendly team.");
    }

    [Fact]
    public async Task Handle_WithoutRatings_StoresAllRatingsAsNull()
    {
        await _handler.Handle(Command(ratings: null), CancellationToken.None);

        _added!.RecruitmentProcessRating.Should().BeNull();
        _added.CommunicationRating.Should().BeNull();
        _added.InterviewExperienceRating.Should().BeNull();
        _added.ProfessionalismRating.Should().BeNull();
    }

    // ---- Guards ----

    [Fact]
    public async Task Handle_WhenNotSignedIn_ThrowsUnauthorized()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFound()
    {
        var command = Command() with { JobApplicationId = Guid.NewGuid() };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenApplicationBelongsToAnotherTalent_ThrowsForbidden()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-2");

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenApplicationIsAGuestApplication_ThrowsForbidden()
    {
        var guestApplication = JobApplication.CreateGuestApplication(_job.Id, "Guest", "guest@example.com", "resumes/g.pdf");
        _applicationRepository
            .Setup(r => r.GetByIdAsync(guestApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestApplication);

        var act = () => _handler.Handle(Command() with { JobApplicationId = guestApplication.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData(CompanyReviewStatus.Pending)]
    [InlineData(CompanyReviewStatus.Withdrawn)]
    public async Task Handle_WhenAReviewAlreadyExists_ThrowsConflict_EvenIfWithdrawn(CompanyReviewStatus status)
    {
        var existing = CompanyReview.Create(_company.Id, _job.Id, _application.Id, TalentId, "Product Designer",
            ReviewSentiment.Positive, null, null, null, null, null);
        if (status == CompanyReviewStatus.Withdrawn)
        {
            existing.Withdraw();
        }

        _reviewRepository
            .Setup(r => r.GetByJobApplicationIdAsync(_application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenThereIsNoAcceptedInterview_ThrowsForbidden()
    {
        SetAcceptedInvitations();

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You can only review a company after an accepted interview.");
        VerifyNothingSaved();
    }

    // ---- Unlock timing ----

    [Fact]
    public async Task Handle_WhenTheInterviewHasNotEndedYet_ThrowsUnprocessableNamingTheUnlockDate()
    {
        var invitation = UpcomingCall();
        SetAcceptedInvitations(invitation);
        var unlockAt = invitation.Timeslots[0].EndTime;

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UnprocessableEntityException>();
        exception.Which.Message.Should().Contain(unlockAt.ToString("MMM d, yyyy"));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenAssessmentDeadlinePlusGraceDaysIsInTheFuture_ThrowsUnprocessable()
    {
        // Deadline was 1 day ago, but with a 2-day grace the review only opens tomorrow.
        _settings.SetupGet(s => s.AssessmentReviewDelayDays).Returns(2);
        SetAcceptedInvitations(Assessment(DateTime.UtcNow.AddDays(-1)));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenAssessmentDeadlinePlusGraceDaysHasPassed_Succeeds()
    {
        _settings.SetupGet(s => s.AssessmentReviewDelayDays).Returns(1);
        SetAcceptedInvitations(Assessment(DateTime.UtcNow.AddDays(-2)));

        await _handler.Handle(Command(), CancellationToken.None);

        _added.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenNoUnlockTimeCanBeDerived_FailsClosed()
    {
        var noSlot = new InterviewInvitation(Guid.NewGuid(), Guid.NewGuid(), EmployerId, TalentId, InterviewType.OnSite);
        noSlot.Accept(null, null);
        SetAcceptedInvitations(noSlot);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WithSeveralRounds_UnlocksAsSoonAsTheEarliestOneHasEnded()
    {
        SetAcceptedInvitations(UpcomingCall(), EndedCall());

        await _handler.Handle(Command(), CancellationToken.None);

        _added.Should().NotBeNull();
    }

    // ---- Company / job resolution ----

    [Theory]
    [MemberData(nameof(JobStates))]
    public async Task Handle_StillWorksWhenTheJobIsClosedExpiredOrDeleted(Action<Job> changeJob)
    {
        changeJob(_job);

        await _handler.Handle(Command(), CancellationToken.None);

        _added.Should().NotBeNull();
        _added!.CompanyId.Should().Be(_company.Id);
        _jobRepository.Verify(r => r.GetByIdIncludingDeletedAsync(_job.Id, It.IsAny<CancellationToken>()), Times.Once);
        _jobRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public static IEnumerable<object[]> JobStates() => new[]
    {
        new object[] { (Action<Job>)(job => { job.MarkAsPublished(); job.MarkAsClosed(); }) },
        new object[] { (Action<Job>)(job => job.MarkAsExpired()) },
        new object[] { (Action<Job>)(job => job.SoftDelete()) }
    };

    [Fact]
    public async Task Handle_WhenTheJobIsGone_ThrowsNotFound()
    {
        _jobRepository
            .Setup(r => r.GetByIdIncludingDeletedAsync(_job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_WhenTheCompanyRowDoesNotExistYet_CreatesItFromTheJobsCompanyName()
    {
        Company? created = null;
        _companyRepository
            .Setup(r => r.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _companyRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((company, _) => created = company)
            .Returns(Task.CompletedTask);

        await _handler.Handle(Command(), CancellationToken.None);

        created.Should().NotBeNull();
        created!.EmployerId.Should().Be(EmployerId);
        created.Name.Should().Be("Design Studio");
        _added!.CompanyId.Should().Be(created.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCompanyRowExists_DoesNotCreateAnother()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        _companyRepository.Verify(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

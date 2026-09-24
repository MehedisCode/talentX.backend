using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetMyCompanyReviews;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetMyCompanyReviews;

public class GetMyCompanyReviewsQueryHandlerTests
{
    private const string TalentId = "talent-1";
    private const string EmployerId = "employer-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInterviewInvitationRepository> _invitationRepository = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ICompanyReviewSettings> _settings = new();
    private readonly GetMyCompanyReviewsQueryHandler _handler;

    private readonly Job _job = Job.Create(
        EmployerId, "Product Designer", "Design Studio", "Description",
        EmploymentType.FullTime, ExperienceLevel.MidLevel, "Remote", WorkArrangement.FullRemote,
        new List<string> { "Figma" }, 1, DateTime.UtcNow.AddDays(30));

    public GetMyCompanyReviewsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_invitationRepository.Object);
        _unitOfWork.SetupGet(u => u.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(u => u.Companies).Returns(_companyRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentId);
        _settings.SetupGet(s => s.AssessmentReviewDelayDays).Returns(1);

        SetInvitations();
        SetReviews();
        SetCompanies();

        _handler = new GetMyCompanyReviewsQueryHandler(_unitOfWork.Object, _currentUserService.Object, _settings.Object);
    }

    // Stands in for the eager-loaded Job that the repository's Include() populates.
    private InterviewInvitation Invitation(
        Guid applicationId,
        InterviewType type = InterviewType.OnCall,
        DateTime? slotEnd = null,
        DateTime? assessmentDeadline = null,
        Job? job = null)
    {
        var invitation = new InterviewInvitation(
            _job.Id, applicationId, EmployerId, TalentId, type, assessmentDeadline: assessmentDeadline);

        if (slotEnd.HasValue)
        {
            invitation.AddTimeslot(slotEnd.Value.AddHours(-1), slotEnd.Value);
            invitation.Accept(invitation.Timeslots[0].Id, invitation.Timeslots[0].StartTime);
        }
        else
        {
            invitation.Accept(null, null);
        }

        typeof(InterviewInvitation).GetProperty(nameof(InterviewInvitation.Job))!
            .SetValue(invitation, job ?? _job);
        return invitation;
    }

    private InterviewInvitation EndedCall(Guid applicationId, int daysAgo = 1) =>
        Invitation(applicationId, slotEnd: DateTime.UtcNow.AddDays(-daysAgo));

    private InterviewInvitation UpcomingCall(Guid applicationId, int daysAhead = 3) =>
        Invitation(applicationId, slotEnd: DateTime.UtcNow.AddDays(daysAhead));

    private CompanyReview NewReview(Guid applicationId, Company? company = null) =>
        CompanyReview.Create(company?.Id ?? Guid.NewGuid(), _job.Id, applicationId, TalentId, "Snapshot Title",
            ReviewSentiment.Negative, "Comment.", 1, 2, null, 4);

    private void SetInvitations(params InterviewInvitation[] invitations) =>
        _invitationRepository
            .Setup(r => r.GetAcceptedByTalentUserIdAsync(TalentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

    private void SetReviews(params CompanyReview[] reviews) =>
        _reviewRepository
            .Setup(r => r.GetByJobApplicationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

    private void SetCompanies(params Company[] companies) =>
        _companyRepository
            .Setup(r => r.GetByEmployerIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);

    private Task<PaginatedList<MyCompanyReviewItemDto>> Query(int page = 1, int pageSize = 20) =>
        _handler.Handle(new GetMyCompanyReviewsQuery(page, pageSize), CancellationToken.None);

    // ---- Basics ----

    [Fact]
    public async Task Handle_WhenNotSignedIn_ThrowsUnauthorized()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => Query();

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithNoAcceptedInterviews_ReturnsAnEmptyPage()
    {
        var result = await Query();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyAsksForTheSignedInTalentsData()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));

        await Query();

        _invitationRepository.Verify(
            r => r.GetAcceptedByTalentUserIdAsync(TalentId, It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepository.Verify(
            r => r.GetAcceptedByTalentUserIdAsync(It.Is<string>(id => id != TalentId), It.IsAny<CancellationToken>()),
            Times.Never);
        _reviewRepository.Verify(
            r => r.GetByJobApplicationIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { application })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Locked / unlocked / reviewed ----

    [Fact]
    public async Task Handle_InterviewNotEndedYet_IsLockedAndShowsTheUnlockDate()
    {
        var application = Guid.NewGuid();
        var invitation = UpcomingCall(application);
        SetInvitations(invitation);

        var item = (await Query()).Items.Single();

        item.JobApplicationId.Should().Be(application);
        item.ReviewUnlocksAt.Should().Be(invitation.Timeslots[0].EndTime);
        item.CanSubmit.Should().BeFalse();
        item.Review.Should().BeNull();
        item.CanEdit.Should().BeFalse();
        item.CanWithdraw.Should().BeFalse();
        item.InterviewType.Should().Be(InterviewType.OnCall);
    }

    [Fact]
    public async Task Handle_InterviewEndedAndNoReviewYet_CanSubmit()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));

        var item = (await Query()).Items.Single();

        item.CanSubmit.Should().BeTrue();
        item.Review.Should().BeNull();
        item.CanEdit.Should().BeFalse();
        item.CanWithdraw.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoUnlockTimeCanBeDerived_IsNotSubmittableWithNullUnlock()
    {
        SetInvitations(Invitation(Guid.NewGuid(), type: InterviewType.OnlineAssessment));

        var item = (await Query()).Items.Single();

        item.ReviewUnlocksAt.Should().BeNull();
        item.CanSubmit.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AssessmentUnlocksAfterDeadlinePlusGraceDays()
    {
        var deadline = DateTime.UtcNow.AddDays(-3);
        SetInvitations(Invitation(Guid.NewGuid(), InterviewType.OnlineAssessment, assessmentDeadline: deadline));

        var item = (await Query()).Items.Single();

        item.ReviewUnlocksAt.Should().Be(deadline.AddDays(1));
        item.CanSubmit.Should().BeTrue();
        item.InterviewType.Should().Be(InterviewType.OnlineAssessment);
    }

    [Fact]
    public async Task Handle_WithAPendingReview_ShowsItAndAllowsEditAndWithdraw()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));
        var review = NewReview(application);
        SetReviews(review);

        var item = (await Query()).Items.Single();

        item.CanSubmit.Should().BeFalse();
        item.CanEdit.Should().BeTrue();
        item.CanWithdraw.Should().BeTrue();
        item.Review!.Id.Should().Be(review.Id);
        item.Review.Status.Should().Be(CompanyReviewStatus.Pending);
        item.Review.Sentiment.Should().Be(ReviewSentiment.Negative);
        item.Review.Comment.Should().Be("Comment.");
        item.Review.Ratings.RecruitmentProcess.Should().Be(1);
        item.Review.Ratings.Communication.Should().Be(2);
        item.Review.Ratings.InterviewExperience.Should().BeNull();
        item.Review.Ratings.Professionalism.Should().Be(4);
        item.Review.SubmittedAt.Should().Be(review.SubmittedAt);
    }

    [Fact]
    public async Task Handle_PublishedReview_HasNoModerationNoteAndCanBeChanged()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));
        var review = NewReview(application);
        review.Approve("admin-1");
        SetReviews(review);

        var item = (await Query()).Items.Single();

        item.Review!.Status.Should().Be(CompanyReviewStatus.Published);
        item.Review.PublishedAt.Should().NotBeNull();
        item.Review.ModerationReason.Should().BeNull();
        item.CanEdit.Should().BeTrue();
        item.CanWithdraw.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RejectedReview_ExposesTheModeratorsReasonAndCanBeChanged()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));
        var review = NewReview(application);
        review.Reject(ReviewModerationReason.PersonalInformation, "Remove the recruiter's name.", "admin-1");
        SetReviews(review);

        var item = (await Query()).Items.Single();

        item.Review!.Status.Should().Be(CompanyReviewStatus.Rejected);
        item.Review.ModerationReason.Should().Be(ReviewModerationReason.PersonalInformation);
        item.Review.ModerationDetails.Should().Be("Remove the recruiter's name.");
        item.CanEdit.Should().BeTrue();
        item.CanWithdraw.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_HiddenReview_ExposesTheReasonButIsLockedForTheTalent()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));
        var review = NewReview(application);
        review.Approve("admin-1");
        review.Hide(ReviewModerationReason.FalseOrMisleading, "Could not be verified.", "admin-1");
        SetReviews(review);

        var item = (await Query()).Items.Single();

        item.Review!.Status.Should().Be(CompanyReviewStatus.Hidden);
        item.Review.ModerationReason.Should().Be(ReviewModerationReason.FalseOrMisleading);
        item.Review.ModerationDetails.Should().Be("Could not be verified.");
        item.CanEdit.Should().BeFalse();
        item.CanWithdraw.Should().BeFalse();
        item.CanSubmit.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithdrawnReview_ShowsNoModerationNoteAndNoActions()
    {
        var application = Guid.NewGuid();
        SetInvitations(EndedCall(application));
        var review = NewReview(application);
        review.Reject(ReviewModerationReason.Spam, "Spam.", "admin-1");
        review.Withdraw();
        SetReviews(review);

        var item = (await Query()).Items.Single();

        item.Review!.Status.Should().Be(CompanyReviewStatus.Withdrawn);
        item.Review.ModerationReason.Should().BeNull();
        item.Review.ModerationDetails.Should().BeNull();
        item.CanEdit.Should().BeFalse();
        item.CanWithdraw.Should().BeFalse();
        item.CanSubmit.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OnlyAttachesTheReviewOfTheMatchingApplication()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        SetInvitations(EndedCall(first), EndedCall(second));
        SetReviews(NewReview(second));

        var items = (await Query()).Items;

        items.Single(i => i.JobApplicationId == first).Review.Should().BeNull();
        items.Single(i => i.JobApplicationId == second).Review.Should().NotBeNull();
    }

    // ---- Multi-round ----

    [Fact]
    public async Task Handle_SeveralAcceptedRounds_ProduceOneItemUsingTheEarliestRound()
    {
        var application = Guid.NewGuid();
        var earlier = Invitation(application, InterviewType.OnSite, slotEnd: DateTime.UtcNow.AddDays(-5));
        var later = Invitation(application, InterviewType.OnCall, slotEnd: DateTime.UtcNow.AddDays(4));
        SetInvitations(later, earlier);

        var result = await Query();

        var item = result.Items.Single();
        result.TotalCount.Should().Be(1);
        item.InterviewType.Should().Be(InterviewType.OnSite);
        item.ReviewUnlocksAt.Should().Be(earlier.Timeslots[0].EndTime);
        item.CanSubmit.Should().BeTrue();
    }

    // ---- Company / job details ----

    [Fact]
    public async Task Handle_UsesTheCompanysCurrentNameAndIdWhenTheCompanyExists()
    {
        var application = Guid.NewGuid();
        var company = new Company(EmployerId, "Design Studio Ltd");
        SetInvitations(EndedCall(application));
        SetCompanies(company);

        var item = (await Query()).Items.Single();

        item.CompanyId.Should().Be(company.Id);
        item.CompanyName.Should().Be("Design Studio Ltd");
        item.JobTitle.Should().Be("Product Designer");
    }

    [Fact]
    public async Task Handle_WhenTheCompanyRowDoesNotExistYet_FallsBackToTheJobsCompanyName()
    {
        SetInvitations(EndedCall(Guid.NewGuid()));

        var item = (await Query()).Items.Single();

        item.CompanyId.Should().BeNull();
        item.CompanyName.Should().Be("Design Studio");
    }

    [Fact]
    public async Task Handle_WithAReview_UsesItsCompanyIdAndJobTitleSnapshot()
    {
        var application = Guid.NewGuid();
        var company = new Company(EmployerId, "Design Studio");
        SetInvitations(EndedCall(application));
        SetCompanies(company);
        SetReviews(NewReview(application, company));

        var item = (await Query()).Items.Single();

        item.CompanyId.Should().Be(company.Id);
        item.JobTitle.Should().Be("Snapshot Title");
    }

    // ---- Ordering and paging ----

    [Fact]
    public async Task Handle_SortsByUnlockDateDescending_WithUnderivableOnesLast()
    {
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var upcoming = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        SetInvitations(
            Invitation(unknown, InterviewType.OnlineAssessment),
            EndedCall(older, daysAgo: 10),
            UpcomingCall(upcoming),
            EndedCall(newer, daysAgo: 2));

        var items = (await Query()).Items;

        items.Select(i => i.JobApplicationId).Should().Equal(upcoming, newer, older, unknown);
    }

    [Fact]
    public async Task Handle_PagesTheResultsInMemory()
    {
        var applications = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        SetInvitations(applications.Select((id, index) => EndedCall(id, daysAgo: index + 1)).ToArray());

        var secondPage = await Query(page: 2, pageSize: 2);

        secondPage.TotalCount.Should().Be(5);
        secondPage.TotalPages.Should().Be(3);
        secondPage.Page.Should().Be(2);
        secondPage.Items.Select(i => i.JobApplicationId).Should().Equal(applications[2], applications[3]);
    }
}

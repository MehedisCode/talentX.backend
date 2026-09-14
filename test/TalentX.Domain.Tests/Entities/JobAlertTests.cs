using System.Reflection;
using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public sealed class JobAlertTests
{
    [Fact]
    public void Create_WhenValidInputsProvided_SetsPropertiesAndIsEnabledTrue()
    {
        var talentIdentifier = "talent-user-789";
        var alertName = "Senior .NET Roles";
        var jobTitle = "Senior Backend Engineer";
        var location = "Remote, Europe";
        var employmentType = EmploymentType.FullTime;
        var experienceLevel = ExperienceLevel.SeniorLevel;
        var workArrangement = WorkArrangement.FullRemote;
        var beforeCreationTimestamp = DateTime.UtcNow;

        var jobAlert = JobAlert.Create(
            talentIdentifier,
            alertName,
            jobTitle,
            location,
            employmentType,
            experienceLevel,
            workArrangement);
        var afterCreationTimestamp = DateTime.UtcNow;

        jobAlert.Id.Should().NotBeEmpty();
        jobAlert.TalentId.Should().Be(talentIdentifier);
        jobAlert.Name.Should().Be(alertName);
        jobAlert.Title.Should().Be(jobTitle);
        jobAlert.Location.Should().Be(location);
        jobAlert.EmploymentType.Should().Be(employmentType);
        jobAlert.ExperienceLevel.Should().Be(experienceLevel);
        jobAlert.WorkArrangement.Should().Be(workArrangement);
        jobAlert.IsEnabled.Should().BeTrue();
        jobAlert.CreatedAt.Should().BeOnOrAfter(beforeCreationTimestamp).And.BeOnOrBefore(afterCreationTimestamp);
        jobAlert.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalledWithNewValues_UpdatesFieldsAndSetsUpdatedAt()
    {
        var jobAlert = JobAlert.Create(
            "talent-user-789",
            "Mid-level Python",
            "Python Developer",
            "London",
            EmploymentType.Contract,
            ExperienceLevel.MidLevel,
            WorkArrangement.Hybrid);

        var updatedName = "Lead Data Engineer";
        var updatedTitle = "Principal Data Engineer";
        var updatedLocation = "Dhaka";
        var updatedEmploymentType = EmploymentType.FullTime;
        var updatedExperienceLevel = ExperienceLevel.Lead;
        var updatedWorkArrangement = WorkArrangement.FullRemote;
        var beforeUpdateTimestamp = DateTime.UtcNow;

        jobAlert.Update(
            updatedName,
            updatedTitle,
            updatedLocation,
            updatedEmploymentType,
            updatedExperienceLevel,
            updatedWorkArrangement);
        var afterUpdateTimestamp = DateTime.UtcNow;

        jobAlert.Name.Should().Be(updatedName);
        jobAlert.Title.Should().Be(updatedTitle);
        jobAlert.Location.Should().Be(updatedLocation);
        jobAlert.EmploymentType.Should().Be(updatedEmploymentType);
        jobAlert.ExperienceLevel.Should().Be(updatedExperienceLevel);
        jobAlert.WorkArrangement.Should().Be(updatedWorkArrangement);
        jobAlert.UpdatedAt.Should().NotBeNull();
        jobAlert.UpdatedAt.Should().BeOnOrAfter(beforeUpdateTimestamp).And.BeOnOrBefore(afterUpdateTimestamp);
    }

    [Fact]
    public void Toggle_WhenCalledWithFalse_DisablesAlertAndSetsUpdatedAt()
    {
        var jobAlert = JobAlert.Create(
            "talent-user-789",
            "Full Stack Roles",
            "Full Stack Developer",
            null,
            null,
            null,
            null);
        var beforeToggleTimestamp = DateTime.UtcNow;

        jobAlert.Toggle(false);
        var afterToggleTimestamp = DateTime.UtcNow;

        jobAlert.IsEnabled.Should().BeFalse();
        jobAlert.UpdatedAt.Should().NotBeNull();
        jobAlert.UpdatedAt.Should().BeOnOrAfter(beforeToggleTimestamp).And.BeOnOrBefore(afterToggleTimestamp);
    }

    [Fact]
    public void Toggle_WhenCalledWithTrue_EnablesAlertAndSetsUpdatedAt()
    {
        var jobAlert = JobAlert.Create(
            "talent-user-789",
            "DevOps Roles",
            "DevOps Engineer",
            null,
            null,
            null,
            null);
        jobAlert.Toggle(false);

        var beforeReenableTimestamp = DateTime.UtcNow;
        jobAlert.Toggle(true);
        var afterReenableTimestamp = DateTime.UtcNow;

        jobAlert.IsEnabled.Should().BeTrue();
        jobAlert.UpdatedAt.Should().NotBeNull();
        jobAlert.UpdatedAt.Should().BeOnOrAfter(beforeReenableTimestamp).And.BeOnOrBefore(afterReenableTimestamp);
    }

    [Fact]
    public void CreateDefault_WhenCalledOnTalentNotificationPreference_SetsTalentIdAndEnablesJobAlertsEmail()
    {
        var talentIdentifier = "talent-user-456";
        var beforeCreationTimestamp = DateTime.UtcNow;

        var preference = TalentNotificationPreference.CreateDefault(talentIdentifier);
        var afterCreationTimestamp = DateTime.UtcNow;

        preference.Id.Should().NotBeEmpty();
        preference.TalentId.Should().Be(talentIdentifier);
        preference.JobAlertsEmailEnabled.Should().BeTrue();
        preference.CreatedAt.Should().BeOnOrAfter(beforeCreationTimestamp).And.BeOnOrBefore(afterCreationTimestamp);
        preference.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WhenCalledOnTalentNotificationPreference_UpdatesFlagAndSetsUpdatedAt()
    {
        var preference = TalentNotificationPreference.CreateDefault("talent-user-456");
        var beforeUpdateTimestamp = DateTime.UtcNow;

        preference.Update(false);
        var afterUpdateTimestamp = DateTime.UtcNow;

        preference.JobAlertsEmailEnabled.Should().BeFalse();
        preference.UpdatedAt.Should().NotBeNull();
        preference.UpdatedAt.Should().BeOnOrAfter(beforeUpdateTimestamp).And.BeOnOrBefore(afterUpdateTimestamp);
    }

    [Fact]
    public void Create_WhenCalledOnJobAlertNotificationHistory_SetsPropertiesAndSentAt()
    {
        var talentIdentifier = "talent-user-123";
        var targetJobId = Guid.NewGuid();
        var targetJobAlertId = Guid.NewGuid();
        var beforeCreationTimestamp = DateTime.UtcNow;

        var historyRecord = JobAlertNotificationHistory.Create(talentIdentifier, targetJobId, targetJobAlertId);
        var afterCreationTimestamp = DateTime.UtcNow;

        historyRecord.Id.Should().NotBeEmpty();
        historyRecord.TalentId.Should().Be(talentIdentifier);
        historyRecord.JobId.Should().Be(targetJobId);
        historyRecord.JobAlertId.Should().Be(targetJobAlertId);
        historyRecord.SentAt.Should().BeOnOrAfter(beforeCreationTimestamp).And.BeOnOrBefore(afterCreationTimestamp);
        historyRecord.CreatedAt.Should().BeOnOrAfter(beforeCreationTimestamp).And.BeOnOrBefore(afterCreationTimestamp);
        historyRecord.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ParameterlessConstructor_JobAlert_ShouldBePresentForEntityFrameworkCore()
    {
        var constructor = typeof(JobAlert).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
    }

    [Fact]
    public void ParameterlessConstructor_TalentNotificationPreference_ShouldBePresentForEntityFrameworkCore()
    {
        var constructor = typeof(TalentNotificationPreference).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
    }

    [Fact]
    public void ParameterlessConstructor_JobAlertNotificationHistory_ShouldBePresentForEntityFrameworkCore()
    {
        var constructor = typeof(JobAlertNotificationHistory).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
    }
}

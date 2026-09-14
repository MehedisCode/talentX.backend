using FluentAssertions;
using RazorLight;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Mailing;
using TalentX.Infrastructure.Mailing.Models;
using Xunit;

namespace TalentX.Infrastructure.Tests.Mailing;

public class EmailTemplateRenderTests
{
    private static readonly IRazorLightEngine Engine = new RazorLightEngineBuilder()
        .UseEmbeddedResourcesProject(typeof(EmailService).Assembly)
        .SetOperatingAssembly(typeof(EmailService).Assembly)
        .Build();

    [Fact]
    public async Task Otp_renders()
    {
        var html = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.Otp",
            new OtpEmailModel("Ayesha", "482913"));

        html.Should().Contain("482913");
        html.Should().Contain("@media only screen");
        html.Should().Contain("Talent<span");
    }

    [Fact]
    public async Task InterviewInvitation_renders()
    {
        var html = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.InterviewInvitation",
            new InterviewInvitationEmailModel("Ayesha", "Senior Backend Engineer", "Northwind Labs"));

        html.Should().Contain("Senior Backend Engineer");
        html.Should().Contain("talentx-stg-app.ait-us.co/interviews");
    }

    [Fact]
    public async Task InterviewConfirmation_renders_with_and_without_instructions()
    {
        var withInstructions = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.InterviewConfirmation",
            new InterviewConfirmationEmailModel("Ayesha", "Senior Backend Engineer", "Northwind Labs",
                "On-site", "Dhaka HQ", "Thu 18 Sep, 2:30 PM", "Bring a photo ID."));

        withInstructions.Should().Contain("Bring a photo ID.");
        withInstructions.Should().Contain("Message from the employer");

        var withoutInstructions = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.InterviewConfirmation",
            new InterviewConfirmationEmailModel("Ayesha", "Senior Backend Engineer", "Northwind Labs",
                "On-site", "Dhaka HQ", "Thu 18 Sep, 2:30 PM", null));

        withoutInstructions.Should().NotContain("Message from the employer");
    }

    [Fact]
    public async Task AccountStatusChanged_renders_for_active_and_deactivated()
    {
        var activated = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged",
            new AccountStatusChangedEmailModel("Ayesha", AccountStatus.Active, UserRole.Talent));

        activated.Should().Contain("Account Activated");
        activated.Should().Contain("reactivated");

        var deactivated = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged",
            new AccountStatusChangedEmailModel("Ayesha", AccountStatus.Deactivated, UserRole.Talent));

        deactivated.Should().Contain("Account Deactivated");
        deactivated.Should().Contain("contact TalentX support");
    }

    [Fact]
    public async Task AccountStatusChanged_deactivation_copy_is_tailored_to_role()
    {
        var employer = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged",
            new AccountStatusChangedEmailModel("Ayesha", AccountStatus.Deactivated, UserRole.Employer));
        employer.Should().Contain("You will not be able to post new jobs until it is reactivated.");

        var talent = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged",
            new AccountStatusChangedEmailModel("Ayesha", AccountStatus.Deactivated, UserRole.Talent));
        talent.Should().Contain("You will not be able to apply to jobs until it is reactivated.");

        var guest = await Engine.CompileRenderAsync(
            "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged",
            new AccountStatusChangedEmailModel("Ayesha", AccountStatus.Deactivated, UserRole.Guest));
        guest.Should().Contain("You will not be able to sign in until it is reactivated.");
    }

    [Fact]
    public async Task JobAlert_renders()
    {
        var html = await Engine.CompileRenderAsync(
            EmailConstants.Templates.JobAlert,
            new JobAlertEmailModel("Ayesha", "Senior C# Engineer", "Acme Corp", "Dhaka", "http://localhost:3000/jobs/123", "C# Remote Alerts"));

        html.Should().Contain("Ayesha");
        html.Should().Contain("Senior C# Engineer");
        html.Should().Contain("Acme Corp");
        html.Should().Contain("Dhaka");
        html.Should().Contain("http://localhost:3000/jobs/123");
        html.Should().Contain("C# Remote Alerts");
    }
}

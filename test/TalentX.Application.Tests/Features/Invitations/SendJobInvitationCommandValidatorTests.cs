using FluentAssertions;
using TalentX.Application.Features.Invitations.Commands.SendJobInvitation;
using Xunit;

namespace TalentX.Application.Tests.Features.Invitations;

public class SendJobInvitationCommandValidatorTests
{
    private readonly SendJobInvitationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldsAreEmpty_Fails()
    {
        var command = new SendJobInvitationCommand(Guid.Empty, string.Empty);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "JobId");
        result.Errors.Should().Contain(e => e.PropertyName == "TalentUserId");
    }

    [Fact]
    public void Validate_WhenFieldsAreValid_Passes()
    {
        var command = new SendJobInvitationCommand(Guid.NewGuid(), "talent-123");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

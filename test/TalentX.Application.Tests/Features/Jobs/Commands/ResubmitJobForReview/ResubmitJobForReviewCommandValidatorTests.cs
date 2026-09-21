using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ResubmitJobForReview;

public sealed class ResubmitJobForReviewCommandValidatorTests
{
    private readonly ResubmitJobForReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoValidationErrors()
    {
        _validator.TestValidate(new ResubmitJobForReviewCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_HasValidationError()
    {
        // The {id:guid} route accepts 00000000-0000-0000-0000-000000000000, so this must be
        // rejected here as a bad request rather than reaching the database as a 404.
        _validator.TestValidate(new ResubmitJobForReviewCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.Id)
            .WithErrorMessage("Job ID is required.");
    }
}

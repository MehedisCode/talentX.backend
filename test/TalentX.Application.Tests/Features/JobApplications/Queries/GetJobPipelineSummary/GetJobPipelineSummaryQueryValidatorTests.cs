using FluentValidation.TestHelper;
using TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetJobPipelineSummary;

public sealed class GetJobPipelineSummaryQueryValidatorTests
{
    private readonly GetJobPipelineSummaryQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenJobIdIsEmpty_ShouldHaveValidationError()
    {
        var query = new GetJobPipelineSummaryQuery(Guid.Empty);

        var validationResult = _validator.TestValidate(query);

        validationResult.ShouldHaveValidationErrorFor(queryInstance => queryInstance.JobId)
            .WithErrorMessage("JobId is required.");
    }

    [Fact]
    public void Validate_WhenJobIdIsValid_ShouldNotHaveValidationError()
    {
        var query = new GetJobPipelineSummaryQuery(Guid.NewGuid());

        var validationResult = _validator.TestValidate(query);

        validationResult.ShouldNotHaveValidationErrorFor(queryInstance => queryInstance.JobId);
    }
}

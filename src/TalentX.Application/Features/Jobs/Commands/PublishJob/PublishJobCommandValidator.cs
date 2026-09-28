using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.PublishJob;

public sealed class PublishJobCommandValidator : AbstractValidator<PublishJobCommand>
{
    public PublishJobCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

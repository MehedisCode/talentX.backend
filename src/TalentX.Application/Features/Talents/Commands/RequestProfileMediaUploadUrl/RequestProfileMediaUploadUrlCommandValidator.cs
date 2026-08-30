using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;

public sealed class RequestProfileMediaUploadUrlCommandValidator : AbstractValidator<RequestProfileMediaUploadUrlCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public RequestProfileMediaUploadUrlCommandValidator()
    {
        RuleFor(command => command.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            .WithMessage("Only JPEG, PNG, and WEBP images are accepted for the profile picture.");
    }
}

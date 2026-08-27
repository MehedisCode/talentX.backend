using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;

public sealed class InitiateMediaMultipartUploadCommandValidator : AbstractValidator<InitiateMediaMultipartUploadCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "video/mp4",
        "video/webm",
        "video/quicktime"
    ];

    public InitiateMediaMultipartUploadCommandValidator()
    {
        RuleFor(command => command.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            .WithMessage("Only MP4, WEBM, and MOV videos are accepted for the video introduction.");
    }
}

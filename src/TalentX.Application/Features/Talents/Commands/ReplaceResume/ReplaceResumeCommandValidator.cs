using FluentValidation;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Talents.Commands.ReplaceResume;

public sealed class ReplaceResumeCommandValidator : AbstractValidator<ReplaceResumeCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".docx"];
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public ReplaceResumeCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A resume file is required.");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("File size must not exceed 5 MB.");

            RuleFor(x => x.File.FileName)
                .Must(name => AllowedExtensions.Contains(
                    Path.GetExtension(name).ToLowerInvariant()))
                .WithMessage("Only PDF and DOCX files are accepted.");

            RuleFor(x => x.File.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType?.ToLowerInvariant()))
                .WithMessage("File content type is not supported.");

            RuleFor(x => x.File)
                .MustAsync(HaveValidMagicBytesAsync)
                .WithMessage("File content does not match its declared type.");
        });
    }

    private static async Task<bool> HaveValidMagicBytesAsync(
        IUploadedFile file, CancellationToken cancellationToken)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.FileName) || file.Length < 4) return false;

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(fileExtension)) return false;

        var stream = file.OpenReadStream();
        if (stream is null) return false;

        var buffer = new byte[4];
        try
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, 4, cancellationToken);
            if (bytesRead < 4) return false;

            return fileExtension switch
            {
                ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
                ".docx" => buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04,
                _ => false
            };
        }
        finally
        {
            if (stream.CanSeek) stream.Position = 0;
        }
    }
}

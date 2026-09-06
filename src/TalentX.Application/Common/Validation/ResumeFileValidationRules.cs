using System.Linq.Expressions;
using FluentValidation;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Common.Validation;

/// <summary>
/// Shared FluentValidation rules for a resume file (size, extension, content-type,
/// magic-byte sniffing) so every command that accepts a raw resume upload
/// (<c>UploadResumeCommand</c>, <c>ExtractResumeCommand</c>, <c>ApplyExtractedProfileCommand</c>)
/// enforces identical rules from one place instead of three copies.
/// </summary>
public static class ResumeFileValidationRules
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".docx", ".doc"];
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public static void ApplyResumeFileValidationRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, IUploadedFile>> fileSelector)
    {
        var compiledFileSelector = fileSelector.Compile();

        validator.RuleFor(fileSelector)
            .NotNull().WithMessage("A resume file is required.");

        validator.When(command => compiledFileSelector(command) is not null, () =>
        {
            validator.RuleFor(command => compiledFileSelector(command).Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("File size must not exceed 5 MB.");

            validator.RuleFor(command => compiledFileSelector(command).FileName)
                .Must(fileName => AllowedExtensions.Contains(
                    Path.GetExtension(fileName).ToLowerInvariant()))
                .WithMessage("Only PDF and DOCX files are accepted.");

            validator.RuleFor(command => compiledFileSelector(command).ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType?.ToLowerInvariant()))
                .WithMessage("File content type is not supported.");

            validator.RuleFor(command => compiledFileSelector(command))
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

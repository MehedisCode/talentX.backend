using System.IO;
using System.Linq;
using FluentValidation;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Companies.Commands.UploadCompanyLogo;

public sealed class UploadCompanyLogoCommandValidator
    : AbstractValidator<UploadCompanyLogoCommand>
{
    private static readonly string[] AllowedExtensions =
    {
        ".png", ".jpg", ".jpeg", ".webp", ".svg"
    };

    private static readonly string[] AllowedContentTypes =
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/svg+xml"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 mb

    public UploadCompanyLogoCommandValidator()
    {
        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("A logo file is required.")
            .Must(file => file.Length > 0)
            .WithMessage("The logo file cannot be empty.")
            .Must(file => file.Length <= MaxFileSize)
            .WithMessage("Logo file size must not exceed 5MB.")
            .Must(IsValidImageFile)
            .WithMessage("Logo must be a valid image file (.png, .jpg, .jpeg, .webp, .svg).");
    }

    private static bool IsValidImageFile(IUploadedFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();

        return AllowedExtensions.Contains(extension)
            && AllowedContentTypes.Contains(contentType);
    }
}
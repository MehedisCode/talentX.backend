using TalentX.Application.Common.Models;

namespace TalentX.Application.Common.Interfaces;

public interface IResumeExtractionService
{
    Task<ResumeExtractionResult> ExtractAsync(
        IUploadedFile file,
        CancellationToken cancellationToken = default);

    Task<ResumeExtractionResult> ExtractAsync(
        IUploadedFile file,
        ResumeExtractionPurpose purpose,
        CancellationToken cancellationToken = default);
}

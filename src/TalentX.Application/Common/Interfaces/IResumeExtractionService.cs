using TalentX.Application.Common.Models;

namespace TalentX.Application.Common.Interfaces;

public interface IResumeExtractionService
{
    Task<ResumeExtractionResult> ExtractAsync(IUploadedFile file, CancellationToken cancellationToken = default);
}

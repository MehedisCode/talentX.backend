using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IEmailDispatchLogRepository
{
    Task AddAsync(EmailDispatchLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailDispatchLog>> ClaimPendingOrRetryableLogsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailDispatchLog>> GetStaleProcessingLogsAsync(TimeSpan staleAfter, CancellationToken cancellationToken = default);
    Task<EmailDispatchLog?> GetLatestByEmailAsync(string recipientEmail, CancellationToken cancellationToken = default);
    void Update(EmailDispatchLog log);
}

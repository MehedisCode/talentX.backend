namespace TalentX.Application.Common.Interfaces;

public interface IJobAlertMatchingService
{
    Task ProcessAlertsForJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}

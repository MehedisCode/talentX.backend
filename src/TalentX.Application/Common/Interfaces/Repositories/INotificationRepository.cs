using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PaginatedList<Notification>> GetByRecipientAsync(
        string recipientUserId,
        int pageNumber,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(string recipientUserId, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(string recipientUserId, CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        return notification;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);
    }

    public async Task<PaginatedList<Notification>> GetByRecipientAsync(
        string recipientUserId,
        int pageNumber,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAt == null);
        }

        query = query.OrderByDescending(notification => notification.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Notification>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(string recipientUserId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(
                notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null,
                cancellationToken);
    }

    public async Task MarkAllAsReadAsync(string recipientUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var unreadNotifications = await _context.Notifications
                .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.MarkAsRead();
            }

            return;
        }

        await _context.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.ReadAt, now)
                .SetProperty(notification => notification.UpdatedAt, now),
                cancellationToken);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class NotificationRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_PersistsNotification()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var notification = Notification.Create(
            "talent-1",
            NotificationType.JobAlertMatch,
            "New matching job",
            "A job matching your alert was posted.",
            "{\"jobId\":\"job-1\"}");

        await repository.AddAsync(notification, CancellationToken.None);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(notification.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.RecipientUserId.Should().Be("talent-1");
        retrieved.Type.Should().Be(NotificationType.JobAlertMatch);
        retrieved.Title.Should().Be("New matching job");
        retrieved.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnreadRowsForRecipient()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var unreadOne = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Title 1", "Body 1", null);
        var unreadTwo = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Title 2", "Body 2", null);
        var read = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Title 3", "Body 3", null);
        read.MarkAsRead();
        var otherRecipientUnread = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Title 4", "Body 4", null);

        await repository.AddAsync(unreadOne, CancellationToken.None);
        await repository.AddAsync(unreadTwo, CancellationToken.None);
        await repository.AddAsync(read, CancellationToken.None);
        await repository.AddAsync(otherRecipientUnread, CancellationToken.None);
        await context.SaveChangesAsync();

        var unreadCount = await repository.GetUnreadCountAsync("talent-1", CancellationToken.None);

        unreadCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByRecipientAsync_ReturnsOnlyGivenRecipientRowsOrderedByCreatedAtDescending()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var first = Notification.Create("talent-1", NotificationType.JobAlertMatch, "First", "Body", null);
        await repository.AddAsync(first, CancellationToken.None);
        await context.SaveChangesAsync();

        var second = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Second", "Body", null);
        await repository.AddAsync(second, CancellationToken.None);
        await context.SaveChangesAsync();

        var otherRecipient = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Other", "Body", null);
        await repository.AddAsync(otherRecipient, CancellationToken.None);
        await context.SaveChangesAsync();

        var result = await repository.GetByRecipientAsync("talent-1", pageNumber: 1, pageSize: 20, unreadOnly: false, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Second");
        result.Items[1].Title.Should().Be("First");
        result.Items.Should().OnlyContain(notification => notification.RecipientUserId == "talent-1");
    }

    [Fact]
    public async Task GetByRecipientAsync_WithUnreadOnlyTrue_ReturnsOnlyUnreadRowsForRecipient()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var unreadNotification = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Unread Title", "Body 1", null);
        var readNotification = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Read Title", "Body 2", null);
        readNotification.MarkAsRead();
        var otherRecipientUnread = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Other Title", "Body 3", null);

        await repository.AddAsync(unreadNotification, CancellationToken.None);
        await repository.AddAsync(readNotification, CancellationToken.None);
        await repository.AddAsync(otherRecipientUnread, CancellationToken.None);
        await context.SaveChangesAsync();

        var result = await repository.GetByRecipientAsync("talent-1", pageNumber: 1, pageSize: 20, unreadOnly: true, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(unreadNotification.Id);
        result.Items[0].Title.Should().Be("Unread Title");
        result.Items[0].ReadAt.Should().BeNull();
        result.Items.Should().OnlyContain(notification => notification.RecipientUserId == "talent-1" && notification.ReadAt == null);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_SetsReadAtOnAllUnreadRowsForRecipient()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var unreadOne = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Title 1", "Body 1", null);
        var unreadTwo = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Title 2", "Body 2", null);
        var otherRecipientUnread = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Title 3", "Body 3", null);

        await repository.AddAsync(unreadOne, CancellationToken.None);
        await repository.AddAsync(unreadTwo, CancellationToken.None);
        await repository.AddAsync(otherRecipientUnread, CancellationToken.None);
        await context.SaveChangesAsync();

        await repository.MarkAllAsReadAsync("talent-1", CancellationToken.None);
        await context.SaveChangesAsync();

        var talentOneNotifications = await context.Notifications
            .Where(notification => notification.RecipientUserId == "talent-1")
            .AsNoTracking()
            .ToListAsync();
        var talentTwoNotifications = await context.Notifications
            .Where(notification => notification.RecipientUserId == "talent-2")
            .AsNoTracking()
            .ToListAsync();

        talentOneNotifications.Should().OnlyContain(notification => notification.ReadAt != null);
        talentTwoNotifications.Should().OnlyContain(notification => notification.ReadAt == null);
    }

    [Fact]
    public async Task DeleteByRecipientIdAsync_DeletesOnlyMatchingRecipient()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var first = Notification.Create("talent-1", NotificationType.JobAlertMatch, "First", "Body", null);
        var second = Notification.Create("talent-1", NotificationType.JobAlertMatch, "Second", "Body", null);
        var otherRecipient = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Other", "Body", null);

        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);
        await repository.AddAsync(otherRecipient, CancellationToken.None);
        await context.SaveChangesAsync();

        await repository.DeleteByRecipientIdAsync("talent-1", CancellationToken.None);
        await context.SaveChangesAsync();

        var remaining = await context.Notifications.AsNoTracking().ToListAsync();

        remaining.Should().ContainSingle();
        remaining[0].RecipientUserId.Should().Be("talent-2");
    }

    [Fact]
    public async Task DeleteByRecipientIdAsync_WhenRecipientHasNoNotifications_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new NotificationRepository(context);

        var notification = Notification.Create("talent-2", NotificationType.JobAlertMatch, "Other", "Body", null);
        await repository.AddAsync(notification, CancellationToken.None);
        await context.SaveChangesAsync();

        await repository.DeleteByRecipientIdAsync("talent-1", CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.Notifications.AsNoTracking().CountAsync()).Should().Be(1);
    }
}

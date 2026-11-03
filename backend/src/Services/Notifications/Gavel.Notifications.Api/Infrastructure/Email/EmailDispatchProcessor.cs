using Gavel.Notifications.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed partial class EmailDispatchProcessor(
    NotificationsDbContext db,
    IEmailSender sender,
    EmailComposer composer,
    TimeProvider clock,
    ILogger<EmailDispatchProcessor> logger)
{
    public const int BatchSize = 20;

    public async Task<int> RunOnceAsync(CancellationToken ct) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var now = clock.GetUtcNow();
            var pending = await db.LockPendingEmailsAsync(now, BatchSize, ct);
            var userIds = pending.Select(n => n.UserId).Distinct().ToList();
            var recipients = await db.Recipients.Where(r => userIds.Contains(r.UserId)).ToDictionaryAsync(r => r.UserId, ct);

            foreach (var notification in pending)
            {
                if (!recipients.TryGetValue(notification.UserId, out var recipient))
                {
                    notification.EmailFailed("The recipient is not known yet.", now);
                    continue;
                }

                try
                {
                    await sender.SendAsync(composer.Compose(notification, recipient), ct);
                    notification.EmailSent(clock.GetUtcNow());
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogSendFailed(logger, exception, notification.Id);
                    notification.EmailFailed(exception.Message, now);
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return pending.Count;
        });

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending the email for notification {NotificationId} failed")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, Guid notificationId);
}

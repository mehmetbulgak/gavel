using Gavel.SharedKernel;

namespace Gavel.Notifications.Api.Domain;

internal sealed class Notification : Entity
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 500;
    public const int MaxEmailAttempts = 5;
    public const int ErrorMaxLength = 500;

    private Notification()
    {
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public Guid? AuctionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public EmailStatus EmailStatus { get; private set; }

    public int EmailAttempts { get; private set; }

    public DateTimeOffset? EmailNextAttemptAt { get; private set; }

    public DateTimeOffset? EmailSentAt { get; private set; }

    public string? EmailLastError { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        Guid? auctionId,
        bool sendEmail,
        DateTimeOffset now)
    {
        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = Truncate(title, TitleMaxLength),
            Body = Truncate(body, BodyMaxLength),
            AuctionId = auctionId,
            CreatedAt = now,
            EmailStatus = sendEmail ? EmailStatus.Pending : EmailStatus.NotRequested,
            EmailNextAttemptAt = sendEmail ? now : null,
        };
    }

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    public void EmailSent(DateTimeOffset now)
    {
        EmailStatus = EmailStatus.Sent;
        EmailSentAt = now;
        EmailNextAttemptAt = null;
        EmailLastError = null;
    }

    public void EmailFailed(string error, DateTimeOffset now)
    {
        EmailAttempts++;
        EmailLastError = Truncate(error, ErrorMaxLength);

        if (EmailAttempts >= MaxEmailAttempts)
        {
            EmailStatus = EmailStatus.Failed;
            EmailNextAttemptAt = null;
            return;
        }

        EmailNextAttemptAt = now.AddSeconds(Math.Pow(2, EmailAttempts) * 15);
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

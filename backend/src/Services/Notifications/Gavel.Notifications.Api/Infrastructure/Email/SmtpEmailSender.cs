using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var settings = options.Value;
        var host = settings.Host ?? throw new InvalidOperationException("Email:Host is not configured.");
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}

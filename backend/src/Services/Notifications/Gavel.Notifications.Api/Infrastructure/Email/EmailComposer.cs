using System.Text.Encodings.Web;
using Gavel.Notifications.Api.Domain;
using Microsoft.Extensions.Options;

namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed class EmailComposer(IOptions<EmailOptions> options)
{
    private static readonly HtmlEncoder Html = HtmlEncoder.Default;

    public EmailMessage Compose(Notification notification, Recipient recipient)
    {
        var baseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
        var link = notification.AuctionId is { } auctionId ? $"{baseUrl}/auctions/{auctionId}" : $"{baseUrl}/notifications";
        var subject = SingleLine(notification.Title);

        var text = $"""
            Hi {recipient.DisplayName},

            {notification.Body}

            {link}

            You are receiving this because you have a Gavel account.
            """;

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="font-family:system-ui,sans-serif;color:#1c1917;line-height:1.5">
              <p>Hi {Html.Encode(recipient.DisplayName)},</p>
              <h2 style="font-size:18px">{Html.Encode(notification.Title)}</h2>
              <p>{Html.Encode(notification.Body)}</p>
              <p><a href="{Html.Encode(link)}">Open Gavel</a></p>
              <p style="color:#78716c;font-size:12px">You are receiving this because you have a Gavel account.</p>
            </body>
            </html>
            """;

        return new EmailMessage(recipient.Email, SingleLine(recipient.DisplayName), subject, text, html);
    }

    private static string SingleLine(string value) => string.Concat(value.Where(c => !char.IsControl(c)));
}

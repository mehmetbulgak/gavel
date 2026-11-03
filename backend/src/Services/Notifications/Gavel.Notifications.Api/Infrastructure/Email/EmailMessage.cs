namespace Gavel.Notifications.Api.Infrastructure.Email;

internal sealed record EmailMessage(string ToAddress, string ToName, string Subject, string TextBody, string HtmlBody);

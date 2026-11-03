using System.Globalization;

namespace Gavel.Notifications.Api.Features.Events;

internal static class NotificationTexts
{
    private static readonly NumberFormatInfo Lira = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };

    public static string Money(decimal amount) =>
        "₺" + amount.ToString(decimal.Truncate(amount) == amount ? "N0" : "N2", Lira);

    public static string Quote(string title) => $"“{title}”";
}

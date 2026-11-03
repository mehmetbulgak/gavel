using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gavel.Notifications.Api.Features.Notifications;

namespace Gavel.Notifications.IntegrationTests;

internal sealed class Inbox(NotificationsFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public Guid UserId { get; } = Guid.NewGuid();

    public HttpClient Client => factory.CreateClientFor(UserId);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task<NotificationPage> ListAsync(bool unreadOnly = false)
    {
        var response = await Client.GetAsync($"/api/notifications?unreadOnly={unreadOnly}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NotificationPage>(Json, Ct))!;
    }

    public async Task<NotificationPage> WaitForAsync(Func<NotificationPage, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var page = await ListAsync();
            if (condition(page) || DateTime.UtcNow > deadline)
            {
                return page;
            }

            await Task.Delay(100, Ct);
        }
    }
}

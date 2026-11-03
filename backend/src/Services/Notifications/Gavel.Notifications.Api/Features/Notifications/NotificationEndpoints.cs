using Gavel.ServiceDefaults.Endpoints;
using Gavel.ServiceDefaults.Http;

namespace Gavel.Notifications.Api.Features.Notifications;

internal sealed class NotificationEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapGet(string.Empty, async ([AsParameters] ListNotifications.Query query, ListNotifications.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(query, ct)))
            .WithValidation<ListNotifications.Query>()
            .Produces<NotificationPage>();

        group.MapGet("/unread-count", async (MarkRead.Handler handler, CancellationToken ct) =>
                TypedResults.Ok(new { count = await handler.UnreadCountAsync(ct) }));

        group.MapPost("/{id:guid}/read", async (Guid id, MarkRead.Handler handler, CancellationToken ct) =>
                (await handler.MarkOneAsync(id, ct)).ToHttpResult())
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/read-all", async (MarkRead.Handler handler, CancellationToken ct) =>
            {
                await handler.MarkAllAsync(ct);
                return TypedResults.NoContent();
            });
    }
}

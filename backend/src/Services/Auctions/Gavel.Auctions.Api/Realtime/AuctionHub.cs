using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gavel.Auctions.Api.Realtime;

[AllowAnonymous]
public sealed class AuctionHub : Hub<IAuctionClient>
{
    public const string Path = "/hubs/auctions";
    public const string LobbyGroup = "lobby";
    public const int MaxSubscriptionsPerConnection = 20;

    private const string SubscriptionsKey = "subscriptions";

    public static string GroupFor(Guid auctionId) => $"auction:{auctionId:N}";

    public async Task SubscribeToAuction(Guid auctionId)
    {
        var subscriptions = Subscriptions();
        if (subscriptions.Contains(auctionId))
        {
            return;
        }

        if (subscriptions.Count >= MaxSubscriptionsPerConnection)
        {
            throw new HubException("Too many auction subscriptions on this connection.");
        }

        subscriptions.Add(auctionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(auctionId));
    }

    public async Task UnsubscribeFromAuction(Guid auctionId)
    {
        if (Subscriptions().Remove(auctionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(auctionId));
        }
    }

    public Task SubscribeToLobby() => Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup);

    public Task UnsubscribeFromLobby() => Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyGroup);

    private HashSet<Guid> Subscriptions()
    {
        if (Context.Items.TryGetValue(SubscriptionsKey, out var existing) && existing is HashSet<Guid> subscriptions)
        {
            return subscriptions;
        }

        subscriptions = [];
        Context.Items[SubscriptionsKey] = subscriptions;
        return subscriptions;
    }
}

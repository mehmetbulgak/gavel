# ADR 0005: Live updates with SignalR, groups and a Redis backplane

**Status:** Accepted

## Context

An auction page is only useful if the price, the bid log and the clock are current. Anonymous visitors should see the
market move; signed-in users should hear about their own outbids and wins wherever they are in the app. Auctions must be
able to run as several instances.

## Decision

- **Auctions hub** (`/hubs/auctions`, anonymous). A page joins the group of the auction it shows, and the home page joins
  a lobby group. A connection can follow at most 20 auctions. After a bid commits, Auctions sends `AuctionChanged` to
  the auction's group with the new price, the minimum next bid, the end time and the new bids, and a slimmer copy without
  the bids to the lobby.
  Bidders appear only as aliases ("Bidder 7"); the reserve price and user ids are never part of a broadcast.
- **Notifications hub** (`/hubs/notifications`, signed in). Notifications are sent with `Clients.User(id)`, where the
  user id comes from the validated token's `sub` claim, so a client can't subscribe to someone else's messages.
- **Redis backplane** for the Auctions hub, so a bid accepted by one instance reaches browsers connected to another.
  Without a Redis connection string the service runs single-instance.
- **The client** merges updates into the TanStack Query cache (details, bid history and every list that shows the
  auction) instead of refetching, dedupes bids by id, and resubscribes after a reconnect. Countdowns use the server's
  clock offset taken from each auction response, so a wrong laptop clock doesn't show the wrong time left.

## Consequences

- Two broadcasts per bid regardless of how many people watch; Redis carries them between instances.
- Messages sent while a client is reconnecting are not replayed. The client refetches on reconnect, so it converges.
- The lobby receives every price change. That is cheap at this scale; a much larger market would push only the
  auctions on the visible page.

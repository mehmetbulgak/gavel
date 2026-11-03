# ADR 0001: Three services behind one gateway

**Status:** Accepted

## Context

An auction house has parts that change for different reasons and fail in different ways. Signing people in must stay
fast and boring. Bidding is the hot path: short transactions on a few popular rows, with every change pushed to many
browsers. Notifications are slow and unreliable by nature (email servers time out) and must never slow a bid down.

## Decision

| Service | Owns | Talks to others through |
|---|---|---|
| Identity | accounts, password hashes, refresh tokens, the token signing key | publishes `UserRegistered`; serves its public key as JWKS |
| Auctions | auctions, bids, participants, watchlists, the auction lifecycle | publishes `BidderOutbid`, `AuctionEndingSoon`, `AuctionEnded` |
| Notifications | notifications and email delivery | consumes the events above |

Each service has its own PostgreSQL database and database role, and no service reads another's tables. A YARP gateway
is the only public entry point: it routes by path, validates tokens so it can rate-limit per user, and strips server
headers. The browser talks to one origin, so cookies stay `SameSite=Strict` and CORS is never needed.

Inside Auctions the code is layered (Domain, Application, Infrastructure, Api) because its rules are the richest. Identity
and Notifications are single projects organised by feature, because a layer split there would be ceremony.

## Consequences

- An email outage or a slow notification handler cannot block bidding; events wait in RabbitMQ.
- Each service can be scaled on its own. Auctions scales out thanks to the Redis backplane ([ADR 0005](0005-realtime-updates.md)).
- Cross-service consistency is eventual. Notifications learns a user's email address from `UserRegistered`; a
  notification for someone it hasn't heard of yet is still stored and shown, and its email is retried until the address
  arrives.
- More moving parts to run locally. docker compose and the Aspire AppHost start everything with one command.

# Gavel

Gavel is a real-time auction application. Sellers list items, bidders set their maximum offer, and the application handles automatic bidding, auction timing and notifications.

## What you can try

- Browse live and upcoming auctions, or list an item for sale.
- Set a private maximum bid and let the system bid only as much as needed.
- Watch prices and bid history update without refreshing the page.
- Follow auctions and receive in-app alerts when outbid or when an auction ends.
- View your bids, listings and watchlist in one place.

Bids in the final two minutes extend the auction. Hidden reserve prices are visible only to the seller, and bidders are shown as anonymous paddle numbers.

## Built with

- **Backend:** C#, ASP.NET Core 10, Entity Framework Core, PostgreSQL.
- **Messaging and live updates:** RabbitMQ, transactional outbox/inbox, SignalR, Redis.
- **Frontend:** React, TypeScript, TanStack Query, Tailwind CSS.
- **Delivery and tests:** Docker, GitHub Actions, xUnit, PostgreSQL integration tests, Vitest.

Identity, Auctions and Notifications run as separate services behind a YARP gateway. Tests cover concurrent bidding, authentication, message delivery and access controls.

## Demo

[Open Gavel](https://gavel-hmkq.onrender.com)

Select **Try the demo account**, or sign in with `demo@gavel.dev` / `Demo1234!`. Automated bidders keep sample auctions active. All bids use virtual money; no purchases or deliveries take place.

The Render setup in `deploy/Dockerfile` runs the services and their dependencies together on one Free instance. Data resets on restart, and the first visit may take a few minutes to load. Email delivery is disabled; in-app notifications remain active. The local Compose setup keeps PostgreSQL 18 and Redis 8, while the bundled demo uses PostgreSQL 16 and Redis 7.

## Run locally

```bash
cp .env.example .env
# Replace the change-me passwords in .env.
docker compose --profile demo up --build
```

Open [localhost:8080](http://localhost:8080).

See the [development guide](docs/development.md) for setup, architecture and API details, and [security notes](SECURITY.md) for deployment considerations.

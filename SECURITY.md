# Security policy

## Reporting a vulnerability

Please do not open a public issue for security problems. Use GitHub's
[private vulnerability reporting](../../security/advisories/new) for this repository instead, with:

- what an attacker can do and which part of the system is affected,
- the steps or a request that reproduces it,
- the commit you tested.

You should get an answer within a week. Gavel is a portfolio project, so there are no bounties, but every valid report is
fixed and credited if you want.

## Things that are intentional

- **The demo account** `demo@gavel.dev` / `Demo1234!` is public on purpose. Only the simulator creates it, and the
  simulator runs only with the `demo` compose profile or the Aspire AppHost. Leave the simulator out of anything that
  isn't a demo.
- **The demo account is shared.** Ten wrong passwords pause it for 15 minutes, like any other account, and replaying an
  old refresh token signs out every demo session. Both only cost visitors a click on "Try the demo account" later.
- **Anyone can watch an auction live.** The auctions hub accepts anonymous connections so visitors see prices move.
  Broadcasts carry bidder aliases, prices and times only: no user ids, names, maximum bids or reserve prices. A connection
  can follow at most 20 auctions.
- **Registration says when an email is taken.** Sign-in doesn't reveal which accounts exist, but sign-up answers
  `409 Auth.EmailTaken`, as most products without email verification do. Sign-up is rate limited per IP.
- **`style-src 'unsafe-inline'`** in the web app's Content Security Policy: the toast library injects its stylesheet at
  runtime and React sets inline styles for the price ticker. Scripts stay limited to `'self'`.
- **Access tokens in the query string for WebSockets.** Browsers can't send headers on a WebSocket handshake, so SignalR
  sends `access_token` as a query parameter. Services accept it only on `/hubs` paths, and nginx doesn't write access logs
  for `/hubs`. Tokens expire after 15 minutes.
- **Plain HTTP inside the compose network**, including the JWKS fetch (`Auth__RequireHttpsMetadata=false`). Only nginx is
  reachable from outside, and only on `127.0.0.1`.

## Deploying it safely

- Put the stack behind a reverse proxy or load balancer that terminates **TLS** and sends **HSTS**. The refresh token
  cookie is `Secure`, so browsers only store it over HTTPS (or on `localhost`).
- Set **`JWT_SIGNING_KEY`** (see the README for the command). Without it Identity creates a new key on every start, which
  signs everyone out, and two Identity instances would not trust each other's tokens.
- Replace every `change-me` value in `.env`, and keep `.env` out of version control (it is git-ignored).
- **Client IPs.** nginx takes the client address from `X-Forwarded-For` only when the request comes from a private
  network (`set_real_ip_from` in `frontend/nginx/nginx.conf`), and the gateway trusts nginx only inside
  `ForwardedHeaders:KnownNetworks`. If your front proxy has a fixed address, narrow both to it; otherwise clients on those
  private ranges could pick their own rate-limit bucket.
- **Several gateway instances.** Rate limits and the per-account sign-in limit are kept in memory per instance. Add a
  limit at the edge proxy too, or move them to a shared store.
- **Email.** Mailpit catches everything in the demo. For real mail set `Email__Host`, `Email__Port`,
  `Email__UseStartTls=true` and credentials on the notifications service.
- Keep dependencies current: Dependabot opens weekly updates for NuGet, npm, Docker images, compose and GitHub Actions.
  Images and Actions are referenced by version tag; pin them to digests and commit SHAs if your policy requires it.

## Known limitations

- **Email addresses are not verified.** Anyone can register with someone else's address and cause notification emails
  to be sent to it. The compose stack delivers all mail to Mailpit; add verification before pointing Gavel at a real
  SMTP server.
- **One RabbitMQ user for every service.** A compromised service could publish any event. Per-service users that may only
  write their own routing keys would contain that.
- **Each service's database role owns its database**, because services apply their migrations at startup. A stricter
  setup runs migrations with a separate owner role and gives the services only read and write rights.
- **Infrastructure containers** (PostgreSQL, Redis, RabbitMQ, Mailpit) run with their images' defaults; only the
  application containers are read-only with capabilities dropped.

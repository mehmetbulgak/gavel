# ADR 0004: ES256 access tokens and rotating refresh cookies

**Status:** Accepted

## Context

Three services and a gateway all need to know who is calling, without a shared secret that every component could also
use to mint tokens, and without a network call to Identity on every request. Browser sessions should survive a reload
without keeping a long-lived credential where JavaScript can read it.

## Decision

- **Access tokens** are ES256 JWTs signed by Identity, valid for 15 minutes, carrying only the user id, display name and
  a token id. Identity publishes the public key at `/.well-known/jwks.json` with a `kid` computed as the RFC 7638
  thumbprint. The gateway and the services validate tokens locally against that key, accept only `ES256`, and check
  issuer, audience and lifetime. A service that sees an unknown `kid` refetches the key set.
- **Refresh tokens** are random 512-bit values in a `__Secure-gavel_rt` cookie (`HttpOnly; Secure; SameSite=Strict;
  Path=/api/auth`), stored as SHA-256 hashes. Every refresh rotates the token. A token reused within 30 seconds of its
  rotation (two tabs refreshing at once) gets a retryable error; reused later, it is treated as stolen and every session
  of that user is revoked. A sign-in session ends after 30 days however often it is refreshed.
- **The SPA** keeps the access token in memory only, refreshes on a 401 or before connecting a hub, shares one refresh
  between concurrent callers, and clears its cache when the signed-in user changes. SignalR WebSockets can't send headers,
  so the token travels as `access_token` in the query string, accepted only on `/hubs` paths and never access-logged.
- **Passwords** use PBKDF2-SHA512 with 210,000 iterations. Unknown emails still pay for a hash comparison, and failed
  sign-ins are limited per account as well as per IP.

## Consequences

- Only Identity can issue tokens. Other components hold a public key, so a compromised gateway can't forge users.
- Revocation takes effect at the next refresh, so a stolen access token is usable for at most 15 minutes.
- Without `Jwt:SigningKey` Identity generates a key at startup and logs a warning: fine for one process in development,
  wrong for anything that restarts or runs twice.

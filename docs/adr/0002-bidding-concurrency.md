# ADR 0002: Serialise bids per auction with a row lock

**Status:** Accepted

## Context

The last minute of a popular auction is a burst of bids on one row. Proxy bidding makes every bid a read-modify-write:
the new price depends on the current leader's private maximum, the increment table, the reserve and the clock. Two bids
handled in parallel on stale data could both "win", or record a history that never happened.

Optimistic concurrency alone (retry on a version conflict) works, but under a burst most attempts lose and retry, which
wastes work exactly when the system is busiest and makes latency unpredictable.

## Decision

A bid runs in one transaction that starts with `SELECT ... FROM auctions WHERE id = @id FOR UPDATE`. Bids for the same
auction therefore queue on the row and see each other's results; bids for different auctions never wait for each other.
Inside the lock the `Auction` aggregate applies the proxy-bidding rules, records the resulting bids, raises the
`BidderOutbid` event into the outbox, and the transaction commits. The live update is broadcast after the commit.

The lifecycle worker (start, "ending soon", close) uses `FOR UPDATE SKIP LOCKED`, so several instances can run it at once
without double-closing an auction, and a close never waits behind a bid burst.

Rules that make the result deterministic:

- The leader's maximum is never revealed. A challenger below it is outbid automatically at the challenger's amount plus
  one increment (capped at the leader's maximum); an equal maximum loses to the earlier bidder.
- A bid inside the final two minutes moves the close to two minutes after that bid.
- Amounts have at most two decimals; the database enforces the same limits with check constraints.

## Consequences

- `Concurrent_bids_on_one_auction_are_applied_one_at_a_time` fires 24 bids at once and checks the price and the history.
- Throughput on a single auction is bounded by one short transaction at a time, far more than a real auction sees.
  Different auctions never contend with each other.
- The aggregate stays a plain C# object with unit tests for every rule; the locking lives in infrastructure.

# ADR 0003: Transactional outbox and inbox over RabbitMQ

**Status:** Accepted

## Context

When a bid is accepted the outbid bidder must be told, and when an auction closes the winner, the seller and the losing
bidders must be told. Publishing to RabbitMQ directly from the request has two failure modes: the transaction commits but
the publish fails (a lost notification), or the publish succeeds and the transaction rolls back (a notification about a
bid that never happened). On the consuming side, RabbitMQ delivers at least once, so a message can arrive twice.

## Decision

**Outbox.** Integration events are written to an `outbox_messages` table in the same transaction as the change that
caused them, together with the W3C trace context. A background publisher reads unprocessed rows with
`FOR UPDATE SKIP LOCKED` (safe with several instances), publishes them as persistent, mandatory messages with publisher
confirms, and marks them processed. It wakes up immediately when a transaction that added outbox rows commits, and
otherwise polls once a second. Failures back off exponentially up to 30 seconds. Processed rows are deleted after a week.

**Broker topology.** One topic exchange, `gavel.events`. Each consuming service has a quorum queue with a delivery limit
and a dead-letter exchange, so a message that keeps failing ends up in `<queue>.dead-letter` instead of blocking the
queue forever.

**Inbox.** A consumer handles a message inside a transaction that also inserts `(message id, consumer)` into
`inbox_messages`. A redelivered message hits the primary key and is acknowledged without running the handler again.
Messages that can't be read at all are rejected straight to the dead-letter queue. Side effects that must not run for an
uncommitted transaction, like pushing a notification over SignalR, are registered as after-commit actions.

## Consequences

- No lost and no phantom events, covered by `Redelivered_messages_are_processed_once`,
  `Unreadable_messages_are_dead_lettered_and_do_not_block_the_queue` and the outbox tests.
- Load testing found that the publisher's own commit used to wake it up again, so it queried the outbox in a tight loop
  and burned half a CPU while idle. The wake-up now fires only for transactions that added outbox rows, and
  `The_publisher_rests_while_there_is_nothing_to_send` keeps it that way.
- Delivery is eventual: a notification typically arrives within milliseconds, but during a RabbitMQ outage it waits in
  the outbox until the broker is back.

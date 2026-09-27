# 03 — Mailbox orchestration shape

**Type:** grilling
**Status:** defined
**Deferred:** 2026-09-27 — Feature-set parked pending Actors as functions that can take all kinds of parameters. May resume around step five or six; intervening gaps unknown.
Blocked by: None

## 1. Question

What is the mailbox and orchestration shape: which long-lived workers, which queues, and where do parsers and graph synchronizers sit relative to CoreMsg?

Provisional aim (Alan 2026-09-27 chat, not yet a Decision): few long-lived workers (filesystem, database) behind queued messages; independent short pieces (parsers, graph synchronizers) do their own thing. Thin routing, not embedded logic in the mailbox.

[[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] says every Core mailbox message finishes quickly; slow work is an Actor. Grill whether that still holds.

Grill:

1. **Long-lived workers** — Are filesystem and database the only long-lived workers for this remake? What do they own versus Core mailbox state?
2. **Queues** — One CoreMsg loop plus worker queues, or another shape? Who enqueues?
3. **Short pieces** — Do parsers and graph synchronizers start as Actors (curried functions) rather than mailbox cases? How do they talk Core without becoming mailbox logic?
4. **Thin routing** — What must stay inside the mailbox (admit, route, persist markers) versus what must leave?

Do not implement.

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.

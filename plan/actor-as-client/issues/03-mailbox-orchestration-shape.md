# 03 — Mailbox orchestration shape

**Type:** grilling
**Status:** done
**Deferred:** 2026-09-27 — Feature-set parked pending Actors as functions that can take all kinds of parameters. May resume around step five or six; intervening gaps unknown.
Blocked by: None
**Actual:** 15m

## 1. Question

What is the mailbox and orchestration shape: which long-lived workers, which queues, and where do parsers and graph synchronizers sit relative to CoreMsg?

Provisional aim (Alan 2026-09-27 chat, not yet a Decision): few long-lived workers (filesystem, database) behind queued messages; independent short pieces (parsers, graph synchronizers) do their own thing. Thin routing, not embedded logic in the mailbox.

[[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] says every Core mailbox message finishes quickly; slow work is an Actor. Grill whether that still holds.

## Answer

1. **Single entry / mailbox as dispatcher** — Everything enters through the mailbox. The mailbox is a dispatcher, not a synchronous executioner: it stamps the Event with a sequence number (EventId) and routes it, but does not wait for completion.
2. **Event sourcing is the authority** — The Event sequence number (EventId) is the watermark — no separate progress counter. A reader asks an entity “did Event N complete?” and waits until yes. Reads are safe by construction; no stale-read guessing.
3. **Open queue access** — Callers may address queues directly (open access). The pipeline is one-directional (graph → file → parser → database), so ordering races across queues are not a concern. The mailbox’s job is routing and stamping, not serializing.
4. **Per-entity completion query** — Each entity that implements an Event can be queried for completion status on that Event.

Worker isolation (in-process with Core versus later out of process) stays open on the map [Not yet specified](../map.md).

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
- 2026-09-27 — Locked from Alan voice. Status `done`. Feature-set remains parked.

## Time

- 2026-09-27 15m — locked mailbox dispatcher, EventId watermark, open queues, entity completion (from chat)

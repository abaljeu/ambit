# 13 — Skip an already-applied Event

**Type:** bug-fixing
**Status:** coded
**Actual:** 45m
**Blocked by:** None — can start immediately

## Context

A Poll, a sync, a Load, or a submit acknowledgement can carry an Event the Browser already applied. Events have event ids. The client event id is the newest applied id. The apply fold runs every Event in the response. An append-style Replace keeps the old child list as a prefix of the new list. A second apply of that Replace appends the new children again. The person then sees duplicate children.

## What to build

The same Event applied twice leaves one copy of each new child.

### 1. Skip an applied Event

1. [x] Already applied — when the Event id is less than or equal to the client event id, do not apply that Event again.
2. [x] Later Events — an Event with a higher id in the same response still applies.
3. [x] Replace stays prefix-capable — an old child list may be a prefix of the new list.

### 2. Want after Events

1. [x] Order — apply Events first, then install the Want answer.
2. [x] No second append — Want install replaces a parent child list with the answer list.

### 3. Proof

1. [x] Replay proof — applying the same Replace-append Event twice through `applyServerTail` leaves one copy.

## Finish-up

1. [x] Piece answers — the build matches this ticket. No piece changed.
2. [x] Basis and reference docs — 9 — SyncLogic already states Event-then-Want order. Current sync docs leave replayed Events unstated. No doc edit.
3. [x] Current docs — no referent in current or user docs states the duplicate apply. Nothing to delete or check off.

## See also

[9 — SyncLogic](../arch.md), [Browser residency](../project.md)

## Comments

- 2026-10-13 — Filed and coded. Skip an Event whose id is already at or below the client event id. Want install stays a list replace.

## Time

- 2026-10-13 45m — skip already-applied Events and the Replace-append replay test (from chat)

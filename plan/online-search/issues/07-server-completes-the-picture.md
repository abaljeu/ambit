# 07 — Server completes the picture

**Status:** `coded`
**Type:** coding
**Actual:** 4h
**Blocked by:** [06 — Residence hits first](06-residence-hits-first.md), [14 — Cap of 200](14-cap-of-200.md)

## Context

The Search Actor reuses the client search on the full server Graph and stops at 200 hits. The gate on [14 — Cap of 200](14-cap-of-200.md) is section 1 **Find and Move**. The Actor home stays [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**.

## What to build

The shared Find and Move Search Actor uses the same client search algorithm. The reply holds up to 200 hits and then stops.

### 1. Search Actor

State, Interface, and Uses for **Search Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 1. This ticket introduces the locked file `src/Server/SearchActor.fs`. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change. Say event source for that log.

1. [x] Actor — The shared Find and Move Search Actor runs in `src/Server/SearchActor.fs`. Move uses this Actor.
2. [x] Shared algorithm — The Actor uses [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs). Today those functions take the search text, the zoom, and the Graph. The client call supplies the residency Graph. The server call supplies the full database. The walk is that same algorithm.
3. [x] Up to 200 hits — The reply holds at most 200 Node ids, then the walk stops. That stop is the client algorithm from [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. The server results exclude Node ids the client search found, and the dialog appends those server hits. Each search stops at 200.
4. [x] One reply — The Actor posts that one reply and stops. There is no continuation cursor.
5. [x] Stale reply — The dialog applies a reply only when it matches the current search text, or an equivalent generation of that text.
6. [x] Event source — `recordStart` records ActorStart. `recordStop` is a queue message. The queue puts ActorStop on the event source. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root. Search does not take a focus. `Api.SearchActorDoor` has `changes`, `recordStart`, and `recordStop`. The mailbox cases are `RecordSearchStart` and `RecordSearchStop`.

## Comments

- 2026-10-06: The Search Actor runs [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) on the full server graph. Find and Move send one Start after the search text settles, and they send none when the client already has 200 hits. The dialog keeps a reply only when the text or generation matches. Query cap is unchanged.
- 2026-10-06: Search Start records ActorStart on the event source, then ActorStop after the one reply. The record supplies root and focus. `graphIds` is the root. The walk reads the full server Graph from State. Query cap is unchanged.
- 2026-10-07: Alan. Search does not take a focus. The server walk uses the root as the zoom. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root. `recordStop` is a queue message. The queue puts ActorStop on the event source. `Api.SearchActorDoor` has `changes`, `recordStart`, and `recordStop`. The mailbox cases are `RecordSearchStart` and `RecordSearchStop`. Poll text "Actor succeeded" stays. Query cap is unchanged.
- 2026-11-13: Alan. Both searches start at the zoom root. This replaces the walk that uses the workspace root as the zoom. [23 — Server search works](23-server-search-works.md) records that `SearchActor.reply` still passes `graph.root`.

## Time

- 2026-10-06 2h — Search Actor reuses the client walk (from chat)
- 2026-10-06 1h — ActorStart on the event source (from chat)
- 2026-10-07 1h — Root-only start and a stop that completes (from chat)

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 3 **Actor file**

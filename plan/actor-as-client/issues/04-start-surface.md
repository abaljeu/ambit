# 04 — Start surface

**Type:** grilling
**Status:** defined
**Deferred:** 2026-09-27 — Feature-set parked pending Actors as functions that can take all kinds of parameters. May resume around step five or six; intervening gaps unknown.
Blocked by: None

## 1. Question

What is the in-process start surface: a Command that posts a curried Actor function, or a named ActorFn registry? What remains of ActorStart and CoreActorPool?

Today CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]) registers `ActorFn` by `ActorName` and `PeerActorName`, and `startActor` takes ActorStart plus a Graph getter. Provisional aim (Alan 2026-09-27 chat, not yet a Decision): in-process start prefers curried functions. A Command constructs an Actor function by closing over data and posts that function to the mailbox or pool. That is more flexible than a narrow ActorStart shape invoked by registered name. Named registry may remain for remote or cross-process start — leave that as fog unless this grill must name one fact now.

Grill:

1. **Curried post** — What does the Command close over (Graph, start ids, worker handles)? What type is posted to the mailbox or pool?
2. **ActorStart** — Does the durable ActorStart Event remain? Which fields stay (`focusId`, `commandId`, `eventId`) once `graphIds` extract is gone?
3. **CoreActorPool** — What remains (live rows, cancel, `sessionId`, deliver) versus what the curried function replaces (named register and start)?
4. **Named registry** — Confirm it stays fog for remote or cross-process start, or name the one fact this remake must lock now.

Do not implement.

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.

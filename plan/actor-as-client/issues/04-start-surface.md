# 04 — Start surface

**Type:** grilling
**Status:** done
**Deferred:** 2026-09-27 — Feature-set parked pending Actors as functions that can take all kinds of parameters. May resume around step five or six; intervening gaps unknown.
Blocked by: None
**Actual:** 15m

## 1. Question

What is the in-process start surface: a Command that posts a curried Actor function, or a named ActorFn registry? What remains of ActorStart and CoreActorPool?

Today CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]) registers `ActorFn` by `ActorName` and `PeerActorName`, and `startActor` takes ActorStart plus a Graph getter. Provisional aim (Alan 2026-09-27 chat, not yet a Decision): in-process start prefers curried functions. A Command constructs an Actor function by closing over data and posts that function to the mailbox or pool. That is more flexible than a narrow ActorStart shape invoked by registered name. Named registry may remain for remote or cross-process start — leave that as fog unless this grill must name one fact now.

## Answer

1. **Curried in-process start** — In-process start uses a curried function posted to the mailbox — not a named ActorFn registry lookup. A Command closes over what the Actor needs and posts that function.
2. **CoreActorPool remains with cancel** — CoreActorPool remains; it retains cancel authority. The information the pool needs to cancel is provided to the function: focus ID (the cancel Focus) and name.
3. **Named registry stays fog** — Named registry for remote or cross-process start stays fog — not locked now.
4. **ActorStart fields** — ActorStart no longer carries the `graphIds` extract (aligned with [02 — Graph handoff](02-graph-handoff.md)’s aim: drop extract; that ticket stays open). It records focus ID and name for the pool’s cancel path.

Live versus snapshot Graph, and which start ids beyond cancel Focus, stay on [02 — Graph handoff](02-graph-handoff.md).

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
- 2026-09-27 — Locked from Alan voice. Status `done`. Feature-set remains parked.

## Time

- 2026-09-27 15m — locked curried post, pool cancel via focus ID and name, registry fog, ActorStart drops graphIds (from chat)

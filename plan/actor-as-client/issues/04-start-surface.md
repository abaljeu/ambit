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
2. **CoreActorPool remains with cancel** — CoreActorPool remains; it retains cancel authority. The pool cancels by focus ID (the cancel Focus) alone, through [112 — Place Cancel through PlaceOrder](../../core-refinement/issues/112-place-cancel-through-placeorder.md). Alan, 2026-11-03.
3. **No in-process registry** — Each endpoint builds its own `FunctionActor`, so there is no in-process registry. Alan, 2026-11-03. Remote or cross-process start is out of scope ([Map](../map.md) section 4).
4. **ActorStart fields** — In the end state, ActorStart no longer carries the `graphIds` extract (aligned with [02 — Graph handoff](02-graph-handoff.md)’s aim: drop extract; that ticket stays open). The Actor start cutover's intermediate state keeps the browser ActorStart payload, `graphIds` included. It records focus ID for the pool’s cancel path.

Live versus snapshot Graph, and which start ids beyond cancel Focus, stay on [02 — Graph handoff](02-graph-handoff.md).

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
- 2026-09-27 — Locked from Alan voice. Status `done`. Feature-set remains parked.
- 2026-11-03 — Alan unparked the Project. Answers 2 and 3 updated. The architecture is on [Arch](../arch.md). Status of this grill stays `done`.

## Time

- 2026-09-27 15m — locked curried post, pool cancel via focus ID and name, registry fog, ActorStart drops graphIds (from chat)

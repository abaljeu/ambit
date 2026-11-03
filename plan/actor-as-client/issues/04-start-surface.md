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

## Decisions 2026-11-03

Alan, 2026-11-03. Detail and the cutover tickets: [Actor as client](../project.md).

[x] `startFunction` is the one pool start. Alan, 2026-11-03.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[o] The pool cancel path requires a name on the function. Answer 2.
[o] A named registry for in-process start stays fog. Answer 3.
[x] The function input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[o] One start takes a function and the FocusId only. The same-day refinement is `startFunction` plus `Order.RunActor`.
[ ] `Order.RunActor` stores the command name and the FocusId.
[o] A new `CoreMsg` case is the start path.
[x] The pool mints the Actor session id.
[ ] The LLM HTTP session is in the AI Actor closure. The dispatcher builds that closure.
[ ] A dispatcher outside Core maps a command name to a function. The pool name tables leave.
[ ] Anything else the Actor needs is in that closure. That includes graph ids and the command node.
[x] The pool runs the function on the .NET thread pool.
[ ] `runSchedule` is renamed `launchPending` on 15 — Delete the old Actor starts.
[ ] Cutover: add `Order.RunActor` beside the old orders. Callers move onto `startFunction`.
[ ] Cutover: delete `startActor`, `startPeerActor`, the name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `Order.StartPeerActor`. `startFunction` stays.

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
- 2026-09-27 — Locked from Alan voice. Status `done`. Feature-set remains parked.
- 2026-11-03 — Alan unparked the Project. The decisions in this file supersede the name-on-the-function answer and the in-process registry fog. Status of this grill stays `done`.

## Time

- 2026-09-27 15m — locked curried post, pool cancel via focus ID and name, registry fog, ActorStart drops graphIds (from chat)

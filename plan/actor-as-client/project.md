# Actor as client

Stage: build
Summary: `startFunction` is the one pool start. An Actor is a thread from the .NET thread pool and runs an actor function. `Order.RunActor` stores the command name and the FocusId.
Updated: 2026-11-03
Started: 2026-09-27
Actual: 2h

**Part of:** [[plan/roadmap/epics/chapters/actors-supported.md]]

## Notes

- 2026-11-03 — Alan unparked this Project. Decisions:

[x] `startFunction` is the one pool start. Alan, 2026-11-03.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[x] The actor function's input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[o] One start takes a function and the FocusId only. The same-day refinement is `startFunction` plus `Order.RunActor`.
[ ] `Order.RunActor` stores the command name and the FocusId.
[o] A new `CoreMsg` case is the start path.
[x] The pool mints the Actor session id.
[ ] The LLM HTTP session is in the AI Actor closure. The dispatcher builds that closure.
[ ] A dispatcher outside Core maps a command name to an actor function. The module is `src/Server/ActorDispatch.fs`. The pool name tables leave.
[ ] Anything else the actor function needs is in that closure. That includes graph ids and the command node.
[x] An Actor is a thread from the .NET thread pool. It runs an actor function.
[o] An Actor is the actor function.
[ ] `runSchedule` is renamed `launchPending` on 15 — Delete the old Actor starts.
[ ] Cutover: add `Order.RunActor` beside the old orders. Callers move onto `startFunction`.
[ ] Cutover: delete `startActor`, `startPeerActor`, the name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand`, `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`. `startFunction` stays. The `Order.StartActor` delete keeps no reader for old rows.
[o] `Order.StartActor` is a logged start. Tickets 07, 08, 09, 10, and 80 write `Order.RunActor` and do not write `Order.StartActor`.
[o] `Order.LoadSave` is the git Load and Save log. Git Load and Save log `Order.RunActor`.
[ ] Find and Move `recordSearchStart` logs `Order.RunActor`. 09 — Dispatch the query command deletes `CoreMsg.RecordSearchStart`. The search-start log is `Order.RunActor`.
[ ] 111 — Place search start as an order, and post SearchStopped shrinks to posting `Notice.SearchStopped` and removing `CoreMsg.RecordSearchStop`. Ambot2 owns it. It runs after 15 — Delete the old Actor starts.
[ ] Ambot2 is writing the `CoreMsg.PostGraphOnly` removal now. That ticket is built before the cutover builds 06 — Log Order.RunActor through 15 — Delete the old Actor starts.
[ ] 112 — Place Cancel through PlaceOrder is being rewritten as a ticket only. It is not ready to build. Its place is after Record the one function Actor start (PR #514).
[ ] Map section 6 records that order. This Project does not edit tickets 111 through 118 in core-refinement.
[o] The browser `ActorStart` payload, including `graphIds`, changes in this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[ ] Remote or cross-process start is out of scope for this cutover. The later question is on the map.
[o] The 2026-09-27 hard cut, and the 2026-09-28 "no new implement tickets" note.

- 2026-09-28 — Alan Server description on [[plan/core-refinement/arch.md]] Target — Server Core confirms Actors as privilege-less external functions that only post to the mailbox. Core and the mailbox own create and destroy, and Graph, file, and git mutations. This Feature-set stays concept-only. No new implement tickets from this lock.
- 2026-09-27 — Deferred pending the reframing of Actors as functions that can take all kinds of parameters. It may land around step five or six; the gaps in between are unknown. Keep the project, map, tickets, and chapter link. Slice one on [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) stays `coded` as historical progress; further slices wait.
- 2026-09-27 — [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md) locked from Alan voice: mailbox is a dispatcher (stamp EventId and route, do not wait); EventId is the watermark; queues are open-access; each entity answers completion on that Event. Feature-set remains deferred.
- 2026-09-27 — [04 — Start surface](issues/04-start-surface.md) locked from Alan voice: curried function posted to the mailbox; CoreActorPool keeps cancel via focus ID and name; named registry stays fog; ActorStart drops `graphIds` and records focus ID and name. Feature-set remains deferred.
- 2026-09-27 — Charted from Alan chat. Existing Core pool and mailbox stay [[plan/core-creation/project.md]]. Parse stays outside Core on [[plan/parse-thread/project.md]]. Parse File tracer stays [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]].
- Map: [[map.md]]. Function-shaped start lookup: [function-shaped start](function-shaped-start.md).
- First grillset: [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md), [02 — Graph handoff](issues/02-graph-handoff.md), [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md), [04 — Start surface](issues/04-start-surface.md).
- 2026-09-27 — Added [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md): sequencing of remake aspects under expand-and-contract. Aims stay provisional.
- 2026-09-27 — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) revised: first slice is function-passing pool setup, not Graph resolver.
- 2026-09-27 — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) step-two working hypothesis only: flexibility across Actor types after function-passing ships. Not a Decision.
- 2026-09-27 — Slice one coded: function-passing pool start expand-alongside. Report: [Slice one: function-passing pool start](reports/slice-one-function-start.md).

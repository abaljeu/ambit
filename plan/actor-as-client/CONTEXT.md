# CONTEXT

This folder is the Project [Actor as client](project.md).

## Sequence

- map: [Map](map.md)
- arch: [Arch](arch.md)
- tickets: [[plan/context/ticket-work2.md]]

## Decisions 2026-11-03

[x] `startFunction` is the one pool start. Alan, 2026-11-03.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[x] The actor function's input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[o] One start takes a function and the FocusId only. The same-day refinement is `startFunction` plus `Order.RunActor`.
[ ] `Order.RunActor` stores the command name and the FocusId.
[o] A new `CoreMsg` case is the start path.
[x] `ActorOrder` includes the actor function. `postActorOrder` puts that order on the mailbox queue. Alan, 2026-11-03.
[x] The pool mints the Actor session id.
[ ] The LLM HTTP session is in the AI Actor closure. The dispatcher builds that closure.
[ ] A dispatcher outside Core maps a command name to an actor function. The module is `src/Server/ActorDispatch.fs`. The pool name tables leave.
[ ] Anything else the actor function needs is in that closure. That includes graph ids and the command node.
[x] An Actor is a thread from the .NET thread pool. It runs an actor function.
[o] An Actor is the actor function.
[ ] `runSchedule` is renamed `launchPending` on 15 — Delete the old Actor starts.
[ ] Cutover: add `Order.RunActor` beside the old orders. Callers move onto `startFunction`.
[ ] Cutover: delete `startActor`, `startPeerActor`, the name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand`, `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`. `startFunction` stays.
[o] `Order.StartActor` is a logged start.
[o] `Order.LoadSave` is the git Load and Save log.
[ ] Find and Move `recordSearchStart` logs `Order.RunActor`. 09 — Dispatch the query command deletes `CoreMsg.RecordSearchStart`. The search-start log is `Order.RunActor`.
[ ] 111 — Place search start as an order, and post SearchStopped shrinks to posting `Notice.SearchStopped` and removing `CoreMsg.RecordSearchStop`. Ambot2 owns it. It runs after 15 — Delete the old Actor starts.
[ ] Ambot2 is writing the `CoreMsg.PostGraphOnly` removal now. That ticket is built before the cutover builds 06 — Log Order.RunActor through 15 — Delete the old Actor starts.
[ ] 112 — Place Cancel through PlaceOrder is being rewritten as a ticket only. It is not ready to build. Its place is after Record the one function Actor start (PR #514).
[ ] Map section 6 records that order. This Project does not edit tickets 111 through 118 in core-refinement.
[o] The browser `ActorStart` payload, including `graphIds`, changes in this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[ ] Remote or cross-process start is out of scope for this cutover.

## Handoff

This file takes over inside `plan/actor-as-client/`.

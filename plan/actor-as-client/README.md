# Actor as client

[Actor as client](project.md) is the Project. [Map](map.md) is the chart. This file is the short entry.

## Decisions 2026-11-03

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
[ ] `runSchedule` is renamed `launchPending` on [15 — Delete the old Actor starts](issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md).
[ ] Cutover: add `Order.RunActor` beside the old orders. Callers move onto `startFunction`.
[ ] Cutover: delete `startActor`, `startPeerActor`, the name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `CoreMsg.StartLoadSaveCommand`, `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave`. `startFunction` stays.
[o] `Order.StartActor` is a logged start.
[o] `Order.LoadSave` is the git Load and Save log.
[ ] Find and Move `recordSearchStart` logs `Order.RunActor`. [09 — Dispatch the query command](issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md) deletes `CoreMsg.RecordSearchStart`. The search-start log is `Order.RunActor`.
[o] The browser `ActorStart` payload, including `graphIds`, changes in this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[ ] Remote or cross-process start is out of scope for this cutover.

## Order

[ ] [111 — Place search start as an order, and post SearchStopped](../core-refinement/issues/111-place-search-start-as-an-order-and-post-searchstopped.md) is owned by Ambot2. It shrinks to posting `Notice.SearchStopped` and removing `CoreMsg.RecordSearchStop`. It runs after 15 — Delete the old Actor starts.
[ ] Ambot2 is writing the `CoreMsg.PostGraphOnly` removal now. That ticket is built before the cutover builds [06 — Log Order.RunActor](issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) through 15 — Delete the old Actor starts.
[ ] [112 — Place Cancel through PlaceOrder](../core-refinement/issues/112-place-cancel-through-placeorder.md) is being rewritten as a ticket only. It is not ready to build. Its place is after [Record the one function Actor start](https://github.com/abaljeu/ambit/pull/514) (PR #514).
[ ] This Project does not edit tickets 111 through 118 in core-refinement. The order is recorded here and on the Map.

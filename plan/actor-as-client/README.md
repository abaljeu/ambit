# Actor as client

[Actor as client](project.md) is the Project. [Map](map.md) is the chart. This file is the short entry.

## Decisions 2026-11-03

[x] `startFunction` is the one pool start. Alan, 2026-11-03.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
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

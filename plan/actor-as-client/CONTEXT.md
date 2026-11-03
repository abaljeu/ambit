# CONTEXT

This folder is the Project [Actor as client](project.md).

## Sequence

- map: [Map](map.md)
- tickets: [[plan/context/ticket-work2.md]]

## Decisions 2026-11-03

[x] `startFunction` exists. `FunctionStart` also carries `getGraph` and `commandId`.
[o] `startActor` looks up a name table.
[o] `startPeerActor` looks up a second name table. The caller passes the name.
[ ] One start takes a function and the FocusId only.
[ ] Graph ids, the session id, and the command node are captured in that function's closure.
[ ] A dispatcher outside Core maps a command name to a function. The pool name tables leave.
[ ] The AI Actor session id is captured in that closure. It is not a function-input field.
[x] The pool runs the function on the .NET thread pool.
[ ] Cutover: add the new start message and its Order beside the old starts.
[ ] Cutover: move every caller onto that start.
[ ] Cutover: delete `startActor`, `startPeerActor`, the name tables, `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `Order.StartPeerActor`.

## Handoff

This file takes over inside `plan/actor-as-client/`.

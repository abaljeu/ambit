# Function-shaped Actor start

Look up new Actor creation here. Decisions: [Actor as client](project.md). Code: [[src/Server/Core/CoreActorPool.fs]].

[x] `startFunction` posts a function. The Actor calls `getGraph`. TestActor has `functionActor`.
[o] `FunctionStart` also carries `getGraph` and `commandId`.
[o] `startActor` looks up a name table. `startPeerActor` looks up a second name table.
[ ] One start takes a function and the FocusId only.
[ ] A dispatcher outside Core maps a command name to that function. The pool name tables leave.
[x] The pool runs the function on the .NET thread pool.

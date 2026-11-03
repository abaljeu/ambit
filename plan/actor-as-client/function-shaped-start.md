# Function-shaped Actor start

Look up new Actor creation here. Decisions: [Actor as client](project.md). Code: [[src/Server/Core/CoreActorPool.fs]].

[x] `startFunction` is the one pool start. The function input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] `Order.RunActor` stores the command name and the FocusId. No new `CoreMsg` case.
[ ] A dispatcher outside Core maps a command name to the function. Anything else the Actor needs is in that closure.
[x] The pool mints the Actor session id and runs the function on the .NET thread pool.

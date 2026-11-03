# Function-shaped Actor start

Look up new Actor creation here. Decisions: [Actor as client](project.md). Code: [[src/Server/Core/CoreActorPool.fs]].

[x] `startFunction` is the one pool start. An Actor is a thread from the .NET thread pool. It runs an actor function.
[x] The actor function's input is `FunctionRun` (`getGraph` and the Actor credential) and `CoreChangesDoor`.
[o] An Actor is the actor function.
[o] `startActor` is the original start.
[o] `startPeerActor` was never supposed to exist.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] `Order.RunActor` stores the command name and the FocusId. No new `CoreMsg` case.
[ ] A dispatcher outside Core maps a command name to an actor function. The module is `src/Server/ActorDispatch.fs`. Anything else the actor function needs is in that closure.
[x] The pool mints the Actor session id.
[o] `Order.StartActor` and `Order.LoadSave` are logged starts.
[o] The browser `ActorStart` payload changes in this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.

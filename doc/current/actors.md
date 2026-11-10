# Actors

Category: Capability

See Also:

[Core](core.md)
[Mailbox](mailbox.md)
[Parse and persist](parse-persist.md)
[Gambol.CloudAgents](gambol-cloud-agents.md)
[Search Actor](search-actor.md)
[Query Actor](query-actor.md)

An Actor is a thread from the .NET thread pool. It runs an actor function.

## Job

[x] Off-queue function. Runs off the mailbox queue and posts fast messages back to the mailbox.
[x] The actor function receives a Graph and posts messages to the mailbox. Core does not own the actor function.
[o] An Actor is the actor function.
[o] Callers register an `ActorFn` by name on the Core actor pool (`src/Server/Core/CoreActorPool.fs`) before the mailbox starts.
[x] `startFunction` is the pool start. The actor function's input is `FunctionRun` (`getGraph` and the credential) and `CoreChangesDoor`. The pool mints the Actor session id.
[o] `startPeerActor` and `CoreMsg.StartPeerActor` were never the start path.
[x] `Order.StartActor` of `ActorOrder` is the logged actor start.
[o] The start provides `ActorStart` of `zoomId`, `focusId`, `actorNodeId`, `graphIds`, and `eventId`. The wire key for `actorNodeId` is `"commandId"`.
[o] The browser `ActorStart` payload changes in this cutover.
[ ] Per-run endpoints: the endpoint varies by what is being run, each with its own payload.
[o] `ActorStop`: a fast mailbox message. Cancel and finish stay on [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
[ ] `Notice.ActorStopped`: a fast mailbox message through the notice function. Cancel and finish stay on [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md). [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 Notice and decision 15 Notice has its own door.
[x] `TestActor`: an injected proof Actor. The name is `test`. File: `src/Server/TestActor.fs`.
[x] A CloudAgents Agent is not an Actor. Detail: Gambol.CloudAgents.
[ ] An Actor requests git Load, git Save, pull, push, or commit by a post to the mailbox. The Actor does not perform that git mutation. Detail: Core.
[x] The architecture summary calls these workers intelligent actors.
[x] Clear-fast rule: Mailbox.
[ ] Find and Move: [Search Actor](search-actor.md).
[ ] Query expression: [Query Actor](query-actor.md).

## Pool

[x] `CoreActorPool` keeps the live table: public Actor identity, secret, termination handle, and Focus. The mailbox is the only thread that reads or writes that table.
[x] `BootActors` is the boot install record: `keys`, `repos`, `grok`, and `search`. The pool stores that record on `bootActors`.
[o] The pool keeps a name table for `startActor` and a second name table for `startPeerActor`.
[x] Persist thread: a thread. Detail: Parse and persist.

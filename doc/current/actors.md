# Actors

Category: Capability

See Also:

[Core](core.md)
[Mailbox](mailbox.md)
[Parse and persist](parse-persist.md)
[Gambol.CloudAgents](gambol-cloud-agents.md)

An Actor is a function outside Core.

## Job

[x] Off-queue function. Runs off the mailbox queue and posts fast messages back to the mailbox.
[x] An Actor receives a Graph and posts messages to the mailbox. Core does not own the Actor body. Callers register an `ActorFn` on Core actor pool (`src/Server/Core/CoreActorPool.fs`) before the mailbox starts.
[x] The mailbox calls `startActor`, records `ActorStart`, and does not wait for the body. The pool schedules the body. Detail: Mailbox.
[x] `ActorStop`: a fast mailbox message. Cancel and finish stay on [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
[x] `TestActor`: an injected proof Actor. The name is `test`. File: `src/Server/TestActor.fs`.
[x] A CloudAgents Agent is not an Actor. Detail: Gambol.CloudAgents.
[ ] An Actor requests git Load, git Save, pull, push, or commit by a post to the mailbox. The Actor does not perform that git mutation. Detail: Core.
[x] The architecture summary calls these workers intelligent actors.
[x] Clear-fast rule: Mailbox.

## Pool

[x] `CoreActorPool` keeps the live table: public Actor identity, secret, termination handle, and Focus. The mailbox is the only thread that reads or writes that table.
[x] Parse thread: a thread. Persist thread: a thread. Detail: Parse and persist.

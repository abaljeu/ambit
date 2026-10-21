# Actors

Category: Capability

See Also:

[Core](core.md)
[Mailbox](mailbox.md)
[Parse and persist](parse-persist.md)
[Gambol.CloudAgents](gambol-cloud-agents.md)
[Search Actor](search-actor.md)
[Query Actor](query-actor.md)

An Actor is a function outside Core.

## Job

[x] Off-queue function. Runs off the mailbox queue and posts fast messages back to the mailbox.
[x] An Actor receives a Graph and posts messages to the mailbox. Core does not own the Actor body. Callers register an `ActorFn` on Core actor pool (`src/Server/Core/CoreActorPool.fs`) before the mailbox starts.
[o] The mailbox calls `startActor`, records `ActorStart`, and does not wait for the body. The pool schedules the body. Detail: Mailbox.
[ ] The mailbox places `Order.StartActor` and records that `Order`. The pool schedules the body. Detail: Mailbox. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 StartActor.
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
[x] Persist thread: a thread. Detail: Parse and persist.

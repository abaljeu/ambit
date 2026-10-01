# Mailbox
Category: Capability
See Also:
- [Core](core.md)
- [File agent](file-agents.md)
- [Db agent](db-agents.md)
- [Actors](actors.md)
- [Parse and persist](parse-persist.md)

The mailbox is the Core loop and the public post path.

## Sources

[CoreMsg](../../src/Server/Core/CoreMsg.fs) - the contract
[Core mailbox](../../src/Server/Core/CoreMailbox.fs) - the queue
[Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs).

## Data

[x] `MailboxProcessor<CoreMsg>`
[ ] The queue element is a private sum of `CoreMsg` and `InMsg`. That sum has no public name.

## Job
[x] The callable door over `MailboxProcessor<CoreMsg>`.
[x] Core mailbox backend: the shared loop. 
[x] Every mailbox message finishes quickly. Work that would hold the mailbox is an Actor. The Actor runs off the queue and posts more fast messages. [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
[x] Mailbox event log: stores `ActorStart` and `ActorStop`. Undo and Redo stay Change-only. Actor events are not Undo targets.

## Interface
[x] `CoreMsg` is internal. Outside Core, a caller does not construct or post `CoreMsg`.
[x] `CoreMailbox` on a `MailboxHost`. Each public function builds a `CoreMsg` and posts through `CoreMailbox.reply` → `MailboxHost.postAndAsyncReply` → the processor pump in `CoreMailboxBackend`.
[x] `CoreMailbox.coreChanges` returns a `CoreChanges` record: `getState`, `getEventId`, `getEventsSince`, `postEvents`, `postGraphOnly`, `actorStop`, `asCaller`.
[x] Server modules call `CoreMailbox` or `CoreChanges`. The client reaches Core only through HTTP routes.

## Internal 
[o] `SnapshotDone` is a `CoreMsg`. Core posts it on the `MailboxProcessor` before `MailboxHost` hides the processor.
[ ] `CoreMsg` has no `SnapshotDone`. `SnapshotDone` is a case of `InMsg`. A private function adds `InMsg` to the mailbox queue. It has no public door.

## Messages
[x] Fast Actor messages on this loop: `StartActor` and `ActorStop`.
[x] Persist handlers do not dispatch Actor cases. The file agent and the db agent do not start a mailbox.
[ ] `InMsg`: internal message type on module Core loop. Not an Op. It carries the completion and the node. Cases: `ParseFinished`, `SnapshotDone`.
[ ] One queue: the mailbox queue. A private function on [Core mailbox](../../src/Server/Core/CoreMailbox.fs) adds `InMsg`. A public function adds `CoreMsg`. The queue puller in [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler. There is no second queue.
[ ] `ParseFinished`: the InMsg handler sets that node Parsed only. The field write is [Graph mutate](../../src/Shared/GraphMutate.fs) `setParseState`. PersistState stays unchanged.
[ ] `SnapshotDone`: the InMsg handler sets that node Persisted. The field write is `GraphMutate.setPersistState`. The same completion stores the snapshot graph as `persistedGraph` when it equals the live graph, clears the in-progress flag, and starts another snapshot when one is needed. The persist thread adds this case through the private function when a file write finishes. The db agent adds this case through the private function when the live-document snapshot finishes.
[ ] Actor post path: `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly`. `InMsg` stays off that path.
[ ] Parsed axis and `PersistState`: internal. Outsiders may see them. The core loop writes them. `Op.SetPersistState` is not a writer. `Op.SetDocumentState` is not the writer of the parsed axis.

## Explanation
The queue stays short so one slow body cannot block every other message. That slow body is an Actor.
The parse thread and the persist thread finish off the loop. An `InMsg` brings that finish back onto the same queue. The puller hands it to the InMsg handler. The handler writes the axis.

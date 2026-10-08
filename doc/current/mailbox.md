# Mailbox
Category: Capability
See Also:
- [Core](core.md)
- [File agent](file-agents.md)
- [Db agent](db-agents.md)
- [Actors](actors.md)
- [Parse and persist](parse-persist.md)
- [Op](op.md)

The mailbox is the Core loop and the public post path.

## Sources

[CoreMsg](../../src/Server/Core/CoreMsg.fs) - the contract
[Core mailbox](../../src/Server/Core/CoreMailbox.fs) - the queue
[Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs).

## Data

[x] The mailbox queue is one `MailboxProcessor`. Its element is a private sum of `CoreMsg` and `InMsg`. That sum has no public name.

## Job
[x] The callable door over the mailbox queue.
[x] Core mailbox backend: the shared loop. 
[x] Every mailbox message finishes quickly. Work that would hold the mailbox is an Actor. The Actor runs off the queue and posts more fast messages. [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
[x] Mailbox event log: stores `ActorStart` and `ActorStop`. Undo and Redo stay Change-only. Actor events are not Undo targets.

## Interface
[x] `CoreMsg` is internal. Outside Core, a caller does not construct or post `CoreMsg`.
[x] `CoreMailbox` on a `MailboxHost`. Each public function builds a `CoreMsg` and posts through `CoreMailbox.reply` → `MailboxHost.postAndAsyncReply` → the processor pump in `CoreMailboxBackend`.
[x] `CoreMailbox.coreChanges` returns a `CoreChanges` record: `getState`, `getEventId`, `getEventsSince`, `postEvents`, `postGraphOnly`, `actorStop`, `asCaller`.
[x] Server modules call `CoreMailbox` or `CoreChanges`. The client reaches Core only through HTTP routes.

## Internal 
[x] `CoreMsg` has no `SnapshotDone`. `SnapshotDone` is a case of `InMsg`. A private function adds `InMsg` to the mailbox queue. It has no public door.

## Messages
[x] Fast Actor messages on this loop: `StartActor` and `ActorStop`.
[x] Persist handlers do not dispatch Actor cases. The file agent and the db agent do not start a mailbox.
[x] `InMsg`: internal message type on module Core loop. Not an Op. It carries the node. Cases: `ParseFinished`, `SnapshotDone`, `MarkUnparsed`.
[x] One queue: the mailbox queue. A private function on [Core mailbox](../../src/Server/Core/CoreMailbox.fs) adds `InMsg`. A public function adds `CoreMsg`. The queue puller in [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler. There is no second queue.
[x] `ParseFinished`: the InMsg handler sets that node Parsed only. The field write is [Graph mutate](../../src/Shared/GraphMutate.fs) `setParseState`. PersistState stays unchanged.
[x] `MarkUnparsed`: the InMsg handler sets that node Unparsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer. The parse thread adds this case through the private function for a disk-newer File Node, and for a Directory Node that Directory reconcile names because it needs reparse.
[x] `SnapshotDone`: the InMsg handler sets that node Persisted through `GraphMutate.setPersistState`, including when the snapshot graph is absent. The same completion stores the snapshot graph as `persistedGraph` when it equals the live graph, clears the in-progress flag, and starts another snapshot when one is needed. The db agent adds this case through the private function when the live-document snapshot finishes. That node is the enclosing workspace of the accepted ops that requested the snapshot.
[ ] The persist thread adds `SnapshotDone` through the private function when a file write finishes.
[x] Actor post path: `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly`. `InMsg` stays off that path.
[ ] Parsed axis and `PersistState`: internal. Outsiders may see them. The core loop writes them. Writer cases: [Op](op.md).

## Explanation
The queue stays short so one slow body cannot block every other message. That slow body is an Actor.
The one mailbox queue is the Event Source order on the server. The puller handles one message, then the next. A later message on that queue does not pass an earlier message. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole. `postEvents` pushes that list onto the queue as one `PostEvent` after another. On ActorStart, a refusal and a fail are the same stored pair: ActorStart and a failed ActorStop. `UnknownActor` uses `unknown actor`. `Rejected` uses that message.
How a route or an Event body changes is [API expansion](api.md#api-expansion). This page does not define a second change protocol.
The parse thread and the persist thread finish off the loop. An `InMsg` brings that finish back onto the same queue. The puller hands it to the InMsg handler. The handler writes the axis.

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
[x] `ParseFinished`: the InMsg handler sets that node Parsed only when `noServerFile` is clear. When `noServerFile` is set, that File Node stays Unparsed. The field write is [Graph mutate](../../src/Shared/GraphMutate.fs) `setParseState`. PersistState stays unchanged.
[x] `MarkUnparsed`: the InMsg handler sets that node Unparsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer. When the legacy enum changes, the handler appends a logged Ev whose body is that op so a client can apply it. Directory reconcile does not name an existing member for this case.
[x] `markUnparsedThenPush` runs only on the mailbox loop. It sets Unparsed, appends that logged Ev when the legacy enum changes, then pushes the parse stack. A caller outside the loop enqueues `InMsg` through the private add. It does not call `writeParseState` or `replaceGraph`.
[x] `SnapshotDone`: the InMsg handler sets that node Persisted through `GraphMutate.setPersistState`, including when the snapshot graph is absent. The same completion stores the snapshot graph as `persistedGraph` when it equals the live graph, clears the in-progress flag, and starts another snapshot when one is needed. The db agent adds this case through the private function when the live-document snapshot finishes. That node is the enclosing workspace of the accepted ops that requested the snapshot.
[ ] The persist thread adds `SnapshotDone` through the private function when a file write finishes.
[x] Actor post path: `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly`. `InMsg` stays off that path.
[x] Parsed axis and `PersistState`: internal. Outsiders may see them. The core loop writes them through `InMsg` and `GraphMutate.setParseState` / `setPersistState`. A changed parse axis is appended as a logged Ev whose body is `Op.SetDocumentState` so a client can apply it. The public post refuses that op. [Op](op.md) is not the writer.

## Explanation
The queue stays short so one slow body cannot block every other message. That slow body is an Actor.
The one mailbox queue is the Event Source order on the server. The puller handles one message, then the next. A later message on that queue does not pass an earlier message. A posted list is applied in order, as one unit; lists from different clients do not interleave. The credential check refuses that event before it applies. An event whose ops contain `SetDocumentState` is refused and does not apply. Events earlier in the same posted list are already on the queue and can already have applied. Otherwise every posted event applies whole. `postEvents` pushes that list onto the queue as one `PostEvent` after another. On ActorStart, a refusal and a fail are the same stored pair: ActorStart and a failed ActorStop. `UnknownActor` uses `unknown actor`. `Rejected` uses that message.
How a route or an Event body changes is [API expansion](api.md#api-expansion). This page does not define a second change protocol.
The parse thread and the persist thread finish off the loop. An `InMsg` brings that finish back onto the same queue. The puller hands it to the InMsg handler. The handler writes the axis.

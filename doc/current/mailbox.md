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
[Session](../../src/Server/Core/Session.fs) - browser authority and admission
[Mailbox log](../../src/Shared/MailboxLog.fs) - `Order`, `Notice`, and the mapping from today's `Ev`
[Core mailbox](../../src/Server/Core/CoreMailbox.fs) - the queue
[Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs).

## Data

The mailbox queue is one `MailboxProcessor`. Its element is internal `CoreQueue`: `Public of Mailbox | Notice of Notice | DropAuthority`.

## Job
[x] The callable door over the mailbox queue.
[x] Core mailbox backend: the shared loop. 
[x] Every mailbox message finishes quickly. Work that would hold the mailbox is an Actor. The Actor runs off the queue and posts more fast messages. [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
[o] Mailbox event log: stores `ActorStart` and `ActorStop`. Undo and Redo stay Change-only. Actor events are not Undo targets.
[ ] Mailbox event log: `EventBody` is `Order of PlacedOrder | Notice of Notice`. `Order.Undo` and `Order.Redo` carry the target `EventId`. `Order.StartActor` and `Notice.ActorStop` are the logged actor cases. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 1 Logged as placed and decision 16 Order carries its envelope fields.

## Interface
A browser route takes a Session admission for the caller before the core order door and finishes that admission after the route queues its order or abandons the post. Actor start admits the new Actor secret when that secret is live in the pool. A later core post copies the caller's Authority onto the stored event.

[o] `CoreMsg` is internal. Outside Core, a caller does not construct or post `CoreMsg`.
[ ] `Mailbox` is the public mailbox and is server-only. A client or an Actor places an `Order` through `Mailbox.PlaceOrder`. The reply is `Result<EventId, string>`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 6 PlaceOrder reply and decision 10 Order, not Command.
[o] `CoreMailbox` on a `MailboxHost`. Each public order builds a `CoreMsg`.

Each public read posts `Mailbox.Ask`. `Query.State` replies with the live Graph and lock-present Focus ids. `Query.LastEventId` replies with the current event id. `Query.EventsSince` replies with the mailbox EventLog after the requested id; event id 0 returns the full log.

`MailboxHost` holds `Session` beside the core mailbox. Login registers the caller's authority and opens admission. Admit checks that the caller may start a route. Logout stops admitting that caller and queues `CoreQueue.DropAuthority` behind orders already on the queue. The route returns at that post. The core removes the authority when it processes `DropAuthority`. At Receive, [Core mailbox load](../../src/Server/Core/CoreMailboxLoad.fs) rejects an order whose current generation `DropAuthority` closed. `PlaceOrder` replies `Error` with a reason. Nothing is applied or logged. A caller with an open generation still runs. Login returns `Ok`. A login while admission is closed opens a new generation, and the queued drop closes only the generation logout queued. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 8 Session leaves.

[ ] Each public order posts `Mailbox.PlaceOrder`.
[o] `CoreMailbox.coreChanges` returns a `CoreChanges` record: `getState`, `getEventId`, `getEventsSince`, `postEvents`, `postGraphOnly`, `actorStop`, `asCaller`.

`CoreMailbox.coreChanges` asks `Query.State`, `Query.LastEventId`, and `Query.EventsSince`. History is `Query.EventsSince` of event id 0.

[ ] `CoreMailbox.coreChanges` places `Order.Change` of ops and `ChangeOrigin` through `Mailbox.PlaceOrder`. `origin` exists only on that placed order. `Notice.ActorStop` goes through the notice function. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 18 ChangeOrigin.
[x] Server modules call `CoreMailbox` or `CoreChanges`. The client reaches Core only through HTTP routes.

## Internal 
[o] `CoreMsg` has no `SnapshotDone`. `SnapshotDone` is a case of `Notice`. A private function adds `Notice` to the mailbox queue. It has no public door.
[ ] `Notice.NodePersisted` of `nodeId: NodeId * version: EventId`. The notice function posts that `Notice` onto `CoreQueue`. Holding the notice function is the permission. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 5 SnapshotDone and decision 15 Notice has its own door.

## Messages
[o] Fast Actor messages on this loop: `StartActor` and `ActorStop`.
[ ] Fast Actor messages on this loop: `Order.StartActor` and `Notice.ActorStop`. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 StartActor and §2 Notice.
[x] Persist handlers do not dispatch Actor cases. The file agent and the db agent do not start a mailbox.
[o] `Notice`: Shared type in [Mailbox log](../../src/Shared/MailboxLog.fs). Not an Op. It carries the node. Cases: `NodeParsed`, `SnapshotDone`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`, each of `nodeId: NodeId`.
[ ] `Notice` cases: `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, `NodePersisting`, `NodePersisted` of `NodeId * EventId`, `NodeUnpersisted` of `NodeId`, and `ActorStop` of `NodeId * ActorResult`. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 Notice and decision 12 NodeUnpersisted.
One queue: internal `CoreQueue` (`Public of Mailbox | Notice of Notice`). The notice function posts a `Notice`. A public function posts `Mailbox`. The queue puller in [Core mailbox load](../../src/Server/Core/CoreMailboxLoad.fs) hands `Public` to the `Mailbox` handler and `Notice` to the `Notice` handler. There is no second queue.
[o] `NodeParsed`: the renamed `ParseFinished`. The Notice handler sets that node Parsed only when `noServerFile` is clear. When `noServerFile` is set, that File Node stays Unparsed. The field write is [Graph mutate](../../src/Shared/GraphMutate.fs) `setParseState`. PersistState stays unchanged. `ParseThread` `finishParse` posts this case.
[ ] `Notice.NodeParsed`: the Notice handler sets that node Parsed only when `noServerFile` is clear. When `noServerFile` is set, that File Node stays Unparsed. The field write is [Graph mutate](../../src/Shared/GraphMutate.fs) `setParseState`. PersistState stays unchanged. `ParseThread` `finishParse` posts this `Notice`. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 Notice.
[o] `NodeUploading` and `NodeReceived`: posted beside `Op.SetDocumentState`. The post does not set state. [44 — Apply document state from Node events](../../plan/core-refinement/issues/44-apply-document-state-from-file-events.md) owns those handlers.
[ ] `Notice.NodeUploading` and `Notice.NodeReceived`: a holder of the notice function posts them. [44 — Apply document state from Node events](../../plan/core-refinement/issues/44-apply-document-state-from-file-events.md) owns those handlers. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 Notice and decision 15 Notice has its own door.
[o] `MarkUnparsed`: the Notice handler sets that node Unparsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer. Directory reconcile does not name an existing member for this case.
[o] `SnapshotDone`: the message carries the node id and no graph. The Notice handler calls `noteDiffDone` with that id, then sets that node Persisted through `GraphMutate.setPersistState`. The diff-count hook clears the in-progress flag and starts another file diff when one is needed. `PersistThread.finishWhenBound` posts `Notice.SnapshotDone` for each node in the submit list after a successful write. `DbAgent` and `FileAgent` only pass the poster in through `bindPersistFinished`.
[ ] `Notice.NodePersisted` of `nodeId` and `version`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 5 SnapshotDone.
[o] The persist thread adds `SnapshotDone` through the private function when a file write finishes.
[ ] The persist thread adds `Notice.NodePersisted` of `nodeId: NodeId * version: EventId` through the notice function when a file write finishes. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 5 SnapshotDone.
[o] Actor post path: `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly`. `Notice` stays off that path.
[ ] Actor post path: `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`. `CoreEventDispatch` reads that `origin` and posts `Notice.NodeUnpersisted`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 18 ChangeOrigin.
[ ] Parsed axis and `PersistState`: internal. Outsiders may see them. The core loop writes them. Writer cases: [Op](op.md).

## Explanation
The queue stays short so one slow body cannot block every other message. That slow body is an Actor.
The one mailbox queue is the Event Source order on the server. The puller handles one message, then the next. A later message on that queue does not pass an earlier message. A posted list is applied in order, as one unit; lists from different clients do not interleave. The mailbox applies that posted list whole.

[o] `postEvents` pushes that list onto the queue as one `PostEvent` after another.
[ ] The events door places each `Order` through `Mailbox.PlaceOrder`. The reply is `Result<EventId, string>`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 6 PlaceOrder reply.

On ActorStart, a refusal and a fail are the same stored pair: ActorStart and a failed ActorStop. `UnknownActor` uses `unknown actor`. `Rejected` uses that message.
How a route or an Event body changes is [API expansion](api.md#api-expansion). This page does not define a second change protocol.

[o] The parse thread and the persist thread finish off the loop. An `Notice` brings that finish back onto the same queue. The puller hands it to the Notice handler. The handler writes the axis.
[ ] The parse thread and the persist thread finish off the loop. A `Notice` brings that finish back onto `CoreQueue`. The puller hands it to the Notice handler. Holding the notice function is the permission to post that `Notice`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 15 Notice has its own door.

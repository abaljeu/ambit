# Operations

Category: Capability

See Also:

[Op](op.md)
[Browser](browser.md)
[Persistence model](persistence-model.md)
[Workspace graph](graph.md)
[Gambol.Shared](gambol-shared.md)

A small set of operations transforms a graph of nodes.

## Job

[x] Graph: pure, directed, and potentially cyclic. File: [Model.fs](../../src/Shared/Model.fs).
[x] Cases and apply: [Op](op.md).
[o] Undo and redo use inverted Ops. The client submits inverses. The server stores the forward log. File: [History](../../src/Shared/History.fs).
[ ] Undo and redo: the client places `Order.Undo` or `Order.Redo` of the target `EventId`. The logged order carries that target. The client and the server resolve that target the same way. File: [History](../../src/Shared/History.fs). [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 1 Logged as placed.

## Change

[o] Change: an Action. Event body carries the Op list. `submissionId`: Guid for deduplication. File: [History](../../src/Shared/History.fs).
[ ] Change: the placed `Order.Change` is `ops: Op list * origin: ChangeOrigin`. The logged Change holds the op list. `orderId`: Guid for deduplication. File: [Mailbox log](../../src/Shared/MailboxLog.fs). [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 18 ChangeOrigin and decision 16 Order carries its envelope fields.

## Model building

[x] Paste and import of text become ops. Files: [Paste.fs](../../src/Shared/Paste.fs), [ImportText.fs](../../src/Shared/ImportText.fs).
[ ] Bulk create-from-outline helpers exist beyond paste and import.

## High-level ops

[x] Browser derives structural delete. That delete promotes children and uses trash. File: [ViewModelDeleteOps.fs](../../src/Shared/ViewModelDeleteOps.fs).
[x] Browser derives paste, move, and search-driven navigation.
[x] Browser handles a wikilink and a `[[filepath]]`, with desktop hints and import.

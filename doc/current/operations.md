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
[x] Undo and redo use inverted Ops. The client submits inverses. The server stores the forward log. File: [History](../../src/Shared/History.fs).

## Change

[x] Change: an Action. Event body carries the Op list. `submissionId`: Guid for deduplication. File: [History](../../src/Shared/History.fs).

## Model building

[x] Paste and import of text become ops. Files: [Paste.fs](../../src/Shared/Paste.fs), [ImportText.fs](../../src/Shared/ImportText.fs).
[ ] Bulk create-from-outline helpers exist beyond paste and import.

## High-level ops

[x] Browser derives structural delete. That delete promotes children and uses trash. File: [ViewModelDeleteOps.fs](../../src/Shared/ViewModelDeleteOps.fs).
[x] Browser derives paste, move, and search-driven navigation.
[x] Browser handles a wikilink and a `[[filepath]]`, with desktop hints and import.

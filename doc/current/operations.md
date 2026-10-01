# Operations

Category: Capability

See Also:

[Browser](browser.md)
[Persistence model](persistence-model.md)
[Workspace graph](workspace-graph.md)
[Gambol.Shared](gambol-shared.md)

A small set of operations transforms a graph of nodes.

## Job

[x] Graph: pure, directed, and potentially cyclic. File: [Model.fs](../../src/Shared/Model.fs).
[x] `NewNode` creates a node.
[x] `SetText` sets text from an old value to a new value.
[x] `SetClasses` sets CSS classes.
[x] `Replace` replaces children at an index. `Replace` covers a parent-child edge and a ref edge.
[x] Undo and redo use `History` and inverted ops. Client submits inverses. Server stores the forward log. File: [History.fs](../../src/Shared/History.fs).

## Change

[x] `Change`: `id`, `changeId`, and `ops`. `changeId`: Guid for deduplication. File: [History.fs](../../src/Shared/History.fs).
[x] Op names: `NewNode`, `SetText`, `SetClasses`, and `Replace(parent, index, oldChildren, newChildren)`.

## Model building

[x] Paste and import of text become ops. Files: [Paste.fs](../../src/Shared/Paste.fs), [ImportText.fs](../../src/Shared/ImportText.fs).
[ ] Bulk create-from-outline helpers exist beyond paste and import.

## High-level ops

[x] Browser derives structural delete. That delete promotes children and uses trash. File: [ViewModelDeleteOps.fs](../../src/Shared/ViewModelDeleteOps.fs).
[x] Browser derives paste, move, and search-driven navigation.
[x] Browser handles a wikilink and a `[[filepath]]`, with desktop hints and import.

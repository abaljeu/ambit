# Operations

Category: Architecture
See Also: [[doc/current/arch.md]], [[doc/current/browser.md]], [[doc/current/persistence-model.md]], [[doc/current/workspace-graph.md]]

A small set of operations transforms a graph of nodes. The graph is a pure, directed, potentially cyclic graph ([[src/Shared/Model.fs]]). Node and Graph fields are [[doc/current/persistence-model.md]]. Workspace special nodes and placement are [[doc/current/workspace-graph.md]].

## Is

**`Change` / `Op`** ([[src/Shared/History.fs]]):

- `NewNode`, `SetText`, `SetClasses`, `Replace(parent, index, oldChildren, newChildren)`
- `Change` has `id`, `changeId` (Guid for dedup), `ops`

Low-level ops (shared Browser and Server):

- [x] create node (`NewNode`)
- [x] set text old/new (`SetText`)
- [x] set CSS classes (`SetClasses`)
- [x] replace children at index (`Replace` — parent-child and ref edges)
- [x] undo/redo via `History` + inverted ops (client submits inverses; server stores forward log)

Model building:

- [x] paste / import text → ops ([[src/Shared/Paste.fs]], [[src/Shared/ImportText.fs]])

High-level ops (derived in the Browser):

- structural delete with promotion, trash ([[src/Shared/ViewModelDeleteOps.fs]])
- paste, move, search-driven navigation
- wikilink / `[[filepath]]` handling (desktop hints + import)

## Should Become

- [ ] bulk create-from-outline helpers (beyond paste/import)

## Where

- [[src/Shared/Model.fs]]
- [[src/Shared/History.fs]]
- [[src/Shared/Paste.fs]], [[src/Shared/ImportText.fs]], [[src/Shared/ViewModelDeleteOps.fs]]

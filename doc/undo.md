# Undo design

## Operational model

Single client, single server. Operations are applied optimistically on the client
first (zero latency) and POSTed to the server asynchronously. The server is
authoritative for persistence and revision numbering, not for conflict resolution.

There is no separate redo endpoint. Redo is a consequence of the Emacs stack model
(see below).

## Core types (`Shared/History.fs`)

```fsharp
type Op =
    | NewNode of nodeId: NodeId * text: string
    | SetText of nodeId: NodeId * oldText: string * newText: string
    | Replace of parentId: NodeId * index: int * oldIds: NodeId list * newIds: NodeId list

type Change = { id: int; ops: Op list }
type History = { past: Change list; future: Change list; nextId: int }
type State   = { graph: Graph; history: History; revision: Revision }
```

- `past` is newest-first. Each undo pops from `past`.
- `future` is newest-first. Each redo pops from `future`.
- `id` is a monotonically increasing int (assigned by the client, confirmed by the server).

## Inversion (`Change.invert`)

`Change.invert` builds the structural inverse of a change: op list is reversed
and each op has old/new swapped.

| Op | Inverse |
|----|---------|
| `NewNode(id, text)` | `NewNode(id, text)` — identity; see note below |
| `SetText(id, old, new)` | `SetText(id, new, old)` |
| `Replace(pid, i, olds, news)` | `Replace(pid, i, news, olds)` |

`NewNode` has no `DeleteNode` counterpart in the op set. Its inverse is left as
itself; if undo-of-undo of a `NewNode` is attempted, `Op.undo` will call
`Graph.setText` with a mismatched state and return `ApplyResult.Invalid`, leaving
the graph unchanged. This is acceptable: split-then-undo-then-undo is an edge case
with low practical impact.

## Emacs stack model (`History.addChange`)

When a new change is committed after one or more undos, the `future` stack is
**not discarded**. Instead it is folded back into `past` as inverse changes:

```
future = [c2; c3]   (most-recently-undone first)
new change = c4

past becomes: [c4; inv(c2); inv(c3); ...old past...]
future becomes: []
```

Subsequent undos traverse: undo c4 → undo inv(c2) ≡ redo c2 → undo inv(c3) ≡ redo c3.
This gives "redo via undo" without a separate redo branch, matching Emacs behaviour.

Classical redo (`History.redo`) still works when no new change has been committed
since the last undo, because `future` is only consumed by `addChange`.

## History transitions

| Action | `past` | `future` |
|--------|--------|----------|
| Commit change `c` (no prior undos) | `c :: past` | `[]` |
| Commit change `c` (after undos, `future = [c2;c3]`) | `c :: inv(c2) :: inv(c3) :: past` | `[]` |
| Undo | pop `c` → `past'`; push `c` → `future` | `c :: future'` |
| Redo | pop `c` → `future'`; push `c` → `past` | — |

## Client flow

1. User action → `Change` built in `Update.fs`.
2. `applyAndPost` applies the change locally via `Change.apply` (zero latency).
3. Change is POSTed to `POST /{file}/changes` asynchronously.
4. Server applies via `History.applyChange`, appends to log, triggers snapshot.
5. Server responds with new `revision`; client dispatches `SubmitResponse revision`.

Undo (Ctrl+Z) and Redo (Ctrl+Y) will follow the same pattern: apply locally,
POST the inverse/reapplication to the server. **Not yet wired to server endpoints.**

## Keybindings

| Key | Action |
|-----|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |

Available in both selection and editing modes.

## Error handling

- If `Change.apply` returns `Invalid`, the history is not mutated and the model
  is returned unchanged.
- Undo/redo on empty stacks returns `ApplyResult.Unchanged`.

## What is not implemented

- Server-side undo/redo endpoints (currently undo/redo only apply locally;
  the server state will diverge until a subsequent normal change is posted).
- Undo of `NewNode` via a `DeleteNode` op.

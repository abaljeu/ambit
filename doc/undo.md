# Undo / Redo design (in-memory)

Goal: support undo/redo across client + server using shared types and pure logic.
This document covers in-memory structures only (no persistence yet).

## Concepts

- **Op**: a single low-level mutation intent (new node, set text, replace children).
- **Change**: a high-level user action, represented as a batch of ops.
- **Undo (high-level)**: reverses a whole Change.
- **Undo (low-level)**: reverses a single Op.

## Shared types (proposed)

- `type Op = NewNode of ... | SetText of ... | Replace of ...`
- `type Change = { id: int; ops: Op list }`
- `type History = { past: Change list; future: Change list }`
- `type State = { graph: Graph; history: History }`

Notes:
- `past` is newest-first (stack). `future` is newest-first (stack).
- `id` can be a monotonically increasing int assigned by the server.

## Core pure functions (shared)

- `applyOp : Op -> Graph -> Result<Graph, string>`
- `invertOp : Op -> Op`
- `applyChange : Change -> Graph -> Result<Graph, string>`
- `invertChange : Change -> Change`

Rules:
- `applyChange` applies ops in order.
- `invertChange` reverses op order and inverts each op.
  - `invertChange { ops = [o1;o2;o3] }` becomes `{ ops = [inv o3; inv o2; inv o1] }`.

## Inversion rules (low-level undo)

- `NewNode(nodeId, ...)` inverts to `DeleteNode(nodeId, snapshot)` OR avoid deletion for now.
  - For now we can choose a simpler rule: do not implement undo for NewNode yet.
- `SetText(nodeId, old, new)` inverts to `SetText(nodeId, new, old)`.
- `Replace(parentId, index, oldIds, newIds)` inverts to
  `Replace(parentId, index, newIds, oldIds)`.

Design note:
- To make inversion possible, ops must carry enough info to reverse.
  - `SetText` already carries both old/new.
  - `Replace` already carries both old/new.

## History transitions

Given `state = { graph; history = { past; future } }`.

- **Commit change** (normal edit)
  - apply `change` to `graph` -> `graph'`
  - `past' = change :: past`
  - `future' = []` (branch cut)

- **Undo change**
  - pop `change` from `past`
  - apply `invertChange change` to `graph` -> `graph'`
  - push `change` onto `future`

- **Redo change**
  - pop `change` from `future`
  - apply `change` to `graph` -> `graph'`
  - push `change` onto `past`

## Client/server responsibilities

Shared:
- Defines `Op`, `Change`, `History`, and the pure apply/invert functions.

Client:
- Produces high-level `Change` records from user intent.
- Applies changes optimistically to local `State`.
- Calls server endpoints:
  - `POST /op/*` or `POST /ops` (batch) returning ack with revision + change id.

Server:
- Authoritative graph + history in memory.
- Applies incoming change batches and assigns `id` (and revision).
- Provides undo/redo endpoints:
  - `POST /op/undo` pops one change and applies its inverse.
  - `POST /op/redo` reapplies one change.

## Error handling

- If applying a change fails, do not mutate history.
- Undo/redo on empty stacks is a no-op or error; pick one and test it.

## Testing checklist

- Applying a change pushes to `past` and clears `future`.
- `apply; undo` returns to original graph.
- `apply; undo; redo` returns to post-apply graph.
- Inverse laws for reversible ops:
  - `applyOp op; applyOp (invertOp op)` restores graph.

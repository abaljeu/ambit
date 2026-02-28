# Plan


## Status
We are implementing [[spec]], following the [[arch]].  All documents are in development.

This plan assumes a lightweight client (hidden-input editing) and a simple server that serves the page and persists ops.
Multi-client sync (N<5 clients) is designed in [[api]] and will be implemented in section 4a.

## 1. client/server approach
DONE.
- Fable with a tiny MVU loop (no React)
- Optional later refactor: adopt Elmish if the homegrown loop grows complex

## 2. Define the core data + ops (shared)
DONE.
- Define `node` and `noderoot`
- Define low-level ops:
	- create node
	- set text old new
	- replace (parent-child relations)
	- undo/redo

## 3. Implement the client skeleton
DONE for MVP text editing; structural editing remains.

- [x] Render visible “lines” from state
- [x] Hidden-input editing loop (keydown/input -> ops)
- [x] Selection model: selected nodeview + span
- [x] Text edit commit/cancel with optimistic `POST /submit`
- [ ] Apply structural edits (Enter new sibling, Tab indent, Shift+Tab outdent)

## 4. Implement the server skeleton

DONE for MVP server + save flow.

- [x] Add JSON serialization for shared types (in Shared/)
    - Encode/decode `Op` (all variants)
    - Encode/decode `Change`
    - Encode/decode `State`/`Graph` (for state endpoint)
    - Encode/decode `NodeId` (Guid)
    - Round-trip tests
- [x] Define API contract (see [[api]])
- [x] Implement revision tracking
    - Revision type (monotonically increasing integer)
    - Revision in server state
    - In-memory history in `State` (no persisted message log in MVP)
- [x] Serve `GET /` with the client assets
- [x] `GET /state` -> graph + revision (JSON)
- [x] `POST /submit` -> apply change with revision tracking (see [[api]])
- [x] `POST /save` -> explicit snapshot write
- [ ] `POST /undo` -> undo with revision tracking (deferred)
- [ ] `POST /redo` -> redo with revision tracking (deferred)
- [ ] `GET /ops?since={revision}` -> get changes since revision (deferred)

## 4a. Multi-client sync (N<5 clients)

MVP baseline: last-write-wins by arrival order (see [[sync-mvp]])
- Client sends `(clientRevision, change)` to server
- Server applies against current state and responds with `(revision, graph)`
- No client-side merging required
- Undo/redo is client-local; inverse changes sent as normal edits

Later: upgrade to merge-based sync (see [[api]])

## 5. Persistence

DONE for MVP scope.

- [x] Snapshot format: single text outline file with tabs for indentation
- [x] Explicit save via `POST /save` (no autosave on every edit)
- [x] Rebuild graph from snapshot on startup
- [x] Keep history in-memory for this phase

- Deferred:
    - Persisted append-only transaction log
    - Multi-file snapshot model
    - Replay ops log after snapshot load

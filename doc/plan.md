# Plan

## Status

We are implementing [[spec]], following the [[arch]]. All documents are in development.

This plan assumes a lightweight client (hidden-input editing) and a simple server that serves the page and persists ops.
Multi-client sync (N<5 clients) is documented in [[sync-mvp]] and [[api]].

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
    - set classes old new
    - replace (parent-child relations)
    - undo/redo

## 3. Implement the client skeleton

DONE for MVP text editing; structural editing partially implemented.

- [x] Render visible “lines” from state
- [x] Hidden-input editing loop (keydown/input -> ops)
- [x] Selection model: selected nodeview + span
- [x] Text edit commit/cancel with optimistic `POST /{pathname}/changes`
- [x] Tab indent / Shift+Tab outdent (`UpdateMove.fs`)
- [x] Enter at cursor: split line / new sibling (`UpdateHelpers.fs`)
- [ ] Remaining structural polish (e.g. full parity with spec for all move/delete flows)

## 4. Implement the server skeleton

DONE for current server + persistence.

- [x] Add JSON serialization for shared types (in Shared/)
    - Encode/decode `Op` (all variants)
    - Encode/decode `Change`, `ChangeBatch`, `ChangeBatchAck`, `PollResponse`
    - Encode/decode `Graph` (for state endpoint)
    - Encode/decode `NodeId` (Guid)
    - Round-trip tests
- [x] Define API contract (see [[api]] — implemented `/ambit/*` section)
- [x] Implement revision tracking
    - Revision type (monotonically increasing integer)
    - Revision in server state
    - Durable append-only log (file `.log` and/or PostgreSQL `changes`)
- [x] Serve app at `GET /ambit` with client assets
- [x] `GET /ambit/state` → graph + revision (JSON)
- [x] `POST /ambit/changes` → apply batch, return ack (see [[api]])
- [x] `GET /ambit/poll?rev=` → revision, build stamps, change tail
- [x] Automatic snapshot persistence (no `POST /save` route)
- [ ] `POST /undo` → server undo (deferred)
- [ ] `POST /redo` → server redo (deferred)
- [ ] `GET /ops?since={revision}` → dedicated changes-since endpoint (deferred; poll tail covers MVP)

## 4a. Multi-client sync (N<5 clients)

Implemented baseline: last-write-wins by arrival order (see [[sync-mvp]], [[api]]).

- Client posts `ChangeBatch` to `POST /{pathname}/changes`
- Server responds with `ChangeBatchAck` (revision + `ackedChangeIds`)
- Client polls `GET /{pathname}/poll?rev=N` for remote changes
- Full graph via `GET /{pathname}/state` when needed
- Undo/redo client-local; inverses sent as normal changes

Later: upgrade to target API in [[api]] (sequences, 409, multi-document).

## 5. Persistence

DONE for current scope.

- [x] Snapshot format: tab-indented outline file
- [x] Append-only change log (file `.log` and/or PostgreSQL `changes`)
- [x] Replay log on startup after snapshot checkpoint
- [x] `Persistence:Mode` — `db` (default) or `file` rollback
- [x] Async snapshot write after accepted changes (not via `POST /save`)

Deferred:

- Multi-file snapshot model (if still desired as a product feature)
- Further DB/projection hardening beyond current schema (see [[doc/future/persistence-vs-domain-model.md]])

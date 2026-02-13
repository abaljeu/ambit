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
In Progress.
- Define `node` and `noderoot`
- Define low-level ops:
	- create node
	- set text old new
	- replace (parent-child relations)
	- undo/redo

## 3. Implement the client skeleton

- Render visible “lines” from state
- Hidden-input editing loop (keydown/input -> ops)
- Selection model: selected nodeview + span
- Apply incremental DOM updates for replace/remove/insert

## 4. Implement the server skeleton

- Add JSON serialization for shared types (in Shared/)
    - Encode/decode `Op` (all variants)
    - Encode/decode `Change`
    - Encode/decode `State`/`Graph` (for state endpoint)
    - Encode/decode `NodeId` (Guid)
    - Round-trip tests
- Define API contract (see [[api]])
- Implement revision tracking
    - Revision type (monotonically increasing integer)
    - Revision in server state
    - Message log (append-only log of all requests)
- Serve `GET /` with the client assets
- `GET /state` -> graph + revision (JSON)
- `POST /op/apply` -> apply change with revision tracking (see [[api]])
- `POST /op/undo` -> undo with revision tracking
- `POST /op/redo` -> redo with revision tracking
- `GET /ops?since={revision}` -> get changes since revision

## 4a. Multi-client sync (N<5 clients)

MVP: last-write-wins per version (see [[sync-mvp]])
- Client sends `(version, change)` to server
- If another client already wrote at that version, server discards previous and keeps new
- Server responds with `(newVersion, graph)` so client replaces local state
- No client-side merging required
- Undo/redo is client-local; inverse changes sent as normal edits

Later: upgrade to merge-based sync (see [[api]])

## 5. Persistence

- Append-only time-reversible ops log
    log = [ change ]
    change = 
        change number
        [ op ]

    op = New Node | Change Text | Insert Children | Remove Children | Undo | Redo

- Snapshot format: files in a directory
    - Each file is a text outline with tabs establishing indentation
    - Graph structure resolves to this format
    - Lines may be:
        - Indentation + text content
        - Indentation + `[[wikilink#label]]` (link to another node)
        - Indentation + `#label` (label/tag)
    - File structure represents the graph hierarchy
    - Each node maps to a line (or set of lines) with appropriate indentation

- Rebuild graph, from snapshot forward, on startup
    - Parse text outline files to reconstruct graph
    - Apply ops log from last snapshot change number

# Plan


## Status
We are implementing [[spec]], following the [[arch]].  All documents are in development.

This plan assumes a lightweight client (hidden-input editing) and a simple server that serves the page and persists ops.
Multi-client support is described below but not to be coded yet.  For now we will assume sequential clients only, so there can never be a conflict to resolve.

## 1. Choose the client/server approach

- Fable with a tiny MVU loop (no React)
- Optional later refactor: adopt Elmish if the homegrown loop grows complex

## 2. Define the core data + ops (shared)

- Define `node` and `noderoot`
- Define low-level ops:
	- create node
	- set text old new
	- replace (parent-child relations)
	- undo/redo

Deliverable: a JSON encoding for ops + state.

## 3. Implement the client skeleton

- Render visible “lines” from state
- Hidden-input editing loop (keydown/input -> ops)
- Selection model: selected nodeview + span
- Apply incremental DOM updates for replace/remove/insert

## 4. Implement the server skeleton

- Serve `GET /` with the client assets
- `GET /state` -> graph + revision
- Implement POST endpoints that correspond 1:1 with ops:
    - `POST /op/new-node`
    - `POST /op/set-text`
    - `POST /op/replace`
    - `POST /op/undo`
    - `POST /op/redo`

## 4a. Multi-device sync (single user)

- Client queues ops locally and POSTs them in the background
- Server assigns revisions and appends to ops log
- Client polls `GET /ops?since=rev` to receive remote ops
- On revision mismatch (`409`), client resyncs (fetch missing ops or reload full state) then retries pending ops

## 5. Persistence

- Append-only time-reversible ops log
    log = [ change ]
    change = 
        change number
        [ op ]

    op = New Node | Change Text | Insert Children | Remove Children | Undo | Redo

- Periodic snapshot text file
    snapshot = 
        change number
        [ node ]
    node = 
        nodeid : Guid
        text
        children : [ nodeid ]

- Rebuild graph, from snapshot forward, on startup

# Architecture

- Client/server architecture with a client-side MVU-style loop
- Bias toward small download size and low conceptual overhead
- Full-stack authored in F# with an immutable domain model
  - Main containers may be mutable; elements should remain immutable

## Architecture overview

This app is structured around a small set of operations (ops) that transform a graph of nodes.

- Client: local-first, applies ops immediately, renders outline, maintains selection
- Server: authoritative log + persistence, assigns revisions, serves sync endpoints
- All major functionality should be tested if easy testing is possible.  This means prefer Shared for code location.

## Directory structure

## Project Structure

| Layer  | Technology                          |
|--------|-------------------------------------|
| Client | F# compiled to JavaScript via Fable |
| Server | ASP.NET Core (minimal API)          |
| Shared | Pure F# domain model                |
| Tests  | xUnit                               |

Rationale: a single src/Test folder tends to mix concerns and makes it harder to run a targeted test suite. Separate test projects let you reference exactly the code under test (Shared vs Server) and keep dependencies clean.

## Client

### Requirements

The client needs to:
- Render “lines” for visible occurrences (respect folding/opened state)
- Capture keys and drive edits via operations
- Maintain selection state (nodeview + span)
- Support undo/redo

### Implementation approach

Because learning F# is a core project goal, the client is authored in F#.

#### Fable with a tiny MVU loop (no React)

Principle: keep the architecture benefits of MVU while avoiding a heavy UI framework.

- Model/update in F# compiled to JS
- Prefer direct DOM calls via `Fable.Browser.Dom` (or similar minimal bindings)
- Implement `update : VM -> Msg -> VM * Cmd list` (or `VM` only, if no cmds)
- Keep dependencies minimal; avoid React stacks

## Server

Server should stay simple:
- Serve the initial page and client assets
- Provide a minimal API for fetching state and applying ops
- Persist a single data type (graph + operations)

### Sync (multi-client, N<5)

Assumption: multiple clients (up to 5) may operate on the same model concurrently.

**MVP** (see [[sync-mvp]]): last-write-wins per version.
- Client sends `(version, change)` to server
- Server discards any previous change at that version, applies the new one
- Server responds with `(newVersion, graph)` — client replaces local state
- No client-side merging; undo/redo is client-local

**Later** (see [[api]]): upgrade to merge-based sync with `remoteChanges`.

### HTTP API

See [[api]] for full API contract.

Summary:
- `GET /` -> HTML
- `GET /state` -> current graph + revision
- `POST /submit` -> apply change with revision tracking
- `POST /undo` -> undo with revision tracking
- `POST /redo` -> redo with revision tracking
- `GET /ops?since={revision}` -> changes since revision

All requests include client revision; server returns new revision and any remote changes.

## Domain model

A pure, directed, potentially cyclic graph.

type node
    - uid
    - name
    - children : [uid]
    - text

noderoot : node

## Operations

### Low-level ops (shared client + server)

- [x] create node
- [x] set text old new
- [ ] replace (establishes parent-child relations)
  - node
  - index
  - [ old guids ]
  - [ new guids ]
- undo
- redo

### Model Building
- [ ] create many nodes from text

### Site/composite model (client)

type sitenode
    - node
    - occurrence : scope
    - opened (include children)
    - children : [nodeview]

root : sitenode

selected : block
    - nodeview
    - span

### High-level ops (derived)

- replace node childspan replacement_ids
  algorithm
  - with node
    - for each occurrence
      - if include_children
        - remove children from index to old guids length
        - create occurrence children
        - add those at index
- [[ link ]]
  - find a node id'd link, and replace the current node with that
- copy/paste links
- edit link id (replace all uses)
- select node, select range

## View
- viewroot
    - nodeview
    - trace
- lines
    - editable for cursor
    - capture all keys
    - recursively add sitenodes, stopping at folded
- updates
    - replace site node -> replace view line
    - remove site node -> remove view lines, including all children.
        - find node; find nextnode; remove lines between these indexes
    - insert site node
        - recursively build from site node
        - insert into array

## Storage (server)

Recommendation: append-only ops log + periodic snapshot.

- Simplest to implement undo/redo and debugging
- Easy to rebuild graph on startup
- Snapshot keeps startup time bounded

### Persistence data

Append-only time-reversible ops log:

- `log = [ change ]`
- `change = (change number, [ op ])`
- `op = New Node | Change Text | Insert Children | Remove Children | Undo | Redo`

Periodic snapshot: files in a directory

- Each file is a text outline with tabs establishing indentation
- Graph structure resolves to this format
- Lines may be:
    - Indentation + text content
    - Indentation + `[[wikilink#label]]` (link to another node)
    - Indentation + `#label` (label/tag)
- File structure represents the graph hierarchy
- Each node maps to a line (or set of lines) with appropriate indentation
- Snapshot includes change number (stored separately or in metadata)

Startup:

- Parse text outline files to reconstruct graph
- Replay log entries after snapshot change number


## Testing plan

Goal: follow TDD (test-first, as-needed) while keeping tests high-value and lightweight.

Workflow (repeat per feature):
- Pick the next smallest behavior (one op, one endpoint, one rendering rule)
- Write the smallest failing test that describes it (RED)
- Implement the minimum code to pass (GREEN)
- Refactor (REFACTOR)

Bias: write tests at the lowest level that still gives confidence.
- Prefer pure functions (ops, reducers, diff planners)
- Add persistence and HTTP tests only when implementing those modules
- Avoid UI automation until it saves time

###  Domain/ops unit tests (highest value)

- `applyOp` / `applyOps` are pure and should be developed test-first.
- Invariants to assert after every op batch:
	- All child uids referenced exist
	- Root exists
	- No duplicate child uid within a single node’s `children` list (unless links require it)
	- `set text old new` fails if `old` doesn’t match current (if using optimistic concurrency)
- Undo/redo correctness:
	- `apply ops; undo;` returns to previous state
	- `apply ops; undo; redo;` returns to post-op state

Tooling recommendation: `Expecto` (+ optional `FsCheck` later for property tests).

###  Serialization tests (contract safety)

- Add JSON tests when the JSON shapes are first introduced:
	- snapshot encoding
	- op batch encoding
	- server response encoding

- JSON round-trip for:
	- state snapshot
	- op batches
	- server responses
- Add a few “golden” JSON samples only once the shapes settle.

###  Persistence tests (ops log + snapshot)

- When implementing persistence, drive it with a replay test:
	- `state0 -> append ops -> snapshot -> restart -> load snapshot + replay -> state1` equals expected
- Crash-safety-lite:
	- tolerate trailing partial line / partial record in ops log (choose one: either tolerate or fail loudly and document)

###  Server tests

Keep server testing minimal by pushing logic into a store module.

- Unit test the store “command handler” function that backs endpoints (test-first per endpoint behavior):
	- `POST /ops` applies ops, increments revision, appends log
	- Revision mismatch returns conflict
- Optional later: integration tests that spin up the ASP.NET Core app in-memory and call endpoints.

###  Client tests (minimal)

- Unit-test pure MVU parts when implementing them:
	- `update` produces correct new model and ops queue
	- “render plan” / line-diff computation (if separated) is correct
- Skip browser automation initially. Add Playwright only if regressions become painful.


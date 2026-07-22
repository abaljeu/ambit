# Architecture

- Client/server architecture with a client-side MVU-style loop

- Bias toward small download size and low conceptual overhead

- Full-stack authored in F# with an immutable domain model

  - Main containers may be mutable; elements should remain immutable

## Architecture overview

This app is structured around a small set of operations (ops) that transform a graph of nodes.

- **Client**: local-first editing, renders outline, maintains selection; syncs via poll + change POST

- **Server**: authoritative graph + revision, append-only change log, serves `/ambit` API and static assets

- **Desktop** (optional): WebView2 shell + local HTTP proxy to the cloud app; local file import only

- **Shared**: pure domain, ops, serialization — preferred home for testable logic

- All major functionality should be tested where easy; prefer `src/Shared` for code location

## Project structure

| Layer   | Project / path              | Technology                          |

|---------|-----------------------------|-------------------------------------|

| Client  | `src/Client`                | F# → JavaScript (Fable)             |

| Server  | `src/Server`                | ASP.NET Core (minimal API), Npgsql  |

| Shared  | `src/Shared`                | Pure F# domain model                |

| Desktop | `src/Desktop`               | .NET WPF + WebView2, local proxy    |

| Tests   | `tests/Shared.Tests`, `tests/Server.Tests` | xUnit              |

Rationale: separate test projects reference only the code under test (Shared vs Server) and keep dependencies clean.

## Directory layout

```

gambol.sln

src/

  Client/          Fable MVU app (compiled to src/Server/wwwroot)

  Server/          HTTP API, FileAgent, DbAgent, auth, static wwwroot

  Shared/          Model, ops, ViewModel, Snapshot, Serialization, …

  Desktop/         WPF host, LocalProxy, AuthStore

tests/

  Shared.Tests/

  Server.Tests/    includes DbAgentTests when TEST_DB_CONNECTION_STRING is set

data/              correlated on-disk document artifacts under DataDir (local dev default)

doc/               architecture, API notes, deployment, future plans

scripts/           desktop.sh, fullstack-build.sh, azure helpers

```

VS Code: default build runs Fable watch + server (`dev: Watch + Run`). Desktop: `desktop: Run` → `scripts/desktop.sh run`.

## Client

### Requirements

The client needs to:

- Render “lines” for visible occurrences (respect folding/opened state)

- Capture keys and drive edits via operations

- Maintain selection state (nodeview + span)

- Support undo/redo (client-local history; inverse changes submitted as normal edits)

### Implementation approach

Because learning F# is a core project goal, the client is authored in F#.

#### Fable with a tiny MVU loop (no React)

Principle: keep the architecture benefits of MVU while avoiding a heavy UI framework.

- Model/update in F# compiled to JS (`src/Client/Update*.fs`, `View.fs`)

- Direct DOM via `Fable.Browser.Dom` (see `other/fable.browser.dom.fs` when needed)

- `update : VM -> Msg -> VM * Cmd list` (or `VM` only when no cmds)

- Minimal dependencies; no React stack

- Served under `/ambit` from server `wwwroot` (Fable `--outDir src/Server/wwwroot`)

When running in the desktop shell, the client talks to `localhost` (local proxy). Graph authority remains the cloud server; desktop adds `/_desktop/*` for capabilities and local file access.

## Desktop

Optional host for users who need local filesystem access while using the same web UI.

- **UI**: WPF `WebView2` loads the local proxy URL (not the cloud origin directly)

- **Proxy** (`src/Desktop/LocalProxy.fs`): forwards `/ambit/*` (and static assets) to the configured cloud base URL; handles `/_desktop/*` locally

- **Capabilities** (`src/Shared/DesktopCapabilities.fs`): `GET /_desktop/capabilities` — what file open/import/export/status and workspace-path resolution the host allows

- **File status**: `POST /_desktop/file-status` with `{ "path": "..." }` returns whether the path is invalid, creatable, an existing file, or an existing folder (supports `//label/relative` workspace paths when mapped)

- **File read (import)**: `GET /_desktop/file?path=...` reads a local file or directory listing and returns ops/text for the client to apply via normal cloud sync

- **File write (export)**: `POST /_desktop/file` with `{ "path": "...", "content": "..." }` writes tab-indented child text to a local file (not directories)

- **Auth**: desktop can store cloud session cookie (`AuthStore.fs`) so the proxied app is authenticated

The desktop host does **not** become a second source of truth for the graph. Full detail:
[[doc/current/desktop-local-files.md]]. Roadmap: [[doc/roadmap/postgres-roadmap.md]] §7.

## Server

The server:

- Serves `GET /ambit` (HTML shell from `gambol.template.html`) and Fable bundles from `wwwroot`

- Exposes JSON API under `/ambit` (state, poll, changes)

- Persists graph via **PostgreSQL**; correlated on-disk artifacts auto-persist from accepted DB state (see Storage)

- Optional cookie auth (`Auth:Username` / `Auth:Password` in config → derived token cookie)

Key modules: `Api.fs` (`AgentHandle`), `FileAgent.fs`, `DbAgent.fs`, `Database.fs`, `DatabaseSetup.fs`, `ChangeLog.fs`, `DocumentLoader.fs`.

### Sync (multi-client, N<5)

Assumption: multiple clients (up to 5) may operate on the same model concurrently.

**Current baseline** (see [[doc/current/sync-mvp.md]]): last-write-wins by arrival order on the server.

- Client polls `GET /ambit/poll?rev={n}` for remote changes since `n`

- Client posts changes to `POST /ambit/changes`

- Server applies change, increments revision, append-only log records payload

- Poll returns incremental `changes`; full graph available via `GET /ambit/state` when needed

- No client-side merging; undo/redo is client-local (inverse ops in submit body)

**Later** (see [[api]]): merge-based sync, 409 conflicts, `remoteChanges`.

### HTTP API (implemented paths)

Canonical contract evolution: [[api]]. Running server uses `/ambit` prefix.

| Method | Path | Purpose |

|--------|------|---------|

| `GET` | `/ambit` | App HTML (build stamps injected) |

| `GET` | `/ambit/state` | `{ revision, graph }` |

| `GET` | `/ambit/poll?rev={n}` | revision, build epochs, `changes` since `n` |

| `POST` | `/ambit/changes` | apply `ChangeBatch` (JSON body); ack only |

| `GET` | `/ambit/login`, `POST /ambit/login`, `GET /ambit/logout` | optional auth |

Deferred vs older docs: `POST /undo`, `POST /redo`, `GET /ops?since=…` as separate endpoints — not exposed; undo is client-side. Workspace Upload / Download transport is WebDAV under `/ambit/dav/{label}/…` ([[doc/roadmap/workspace-file-sync]], server surface [[doc/roadmap/workspace-webdav]]).

## Domain model

A pure, directed, potentially cyclic graph (`src/Shared/Model.fs`).

**`Node`**

- `id` : `NodeId` (`Guid`)

- `text`, `name` (`string option`)

- `children` : `ChildNode list` — each child has `ref: Ownership` (`Owner` | `Ref`) and `id`

- `cssClasses`, `owner`, `kind` (`Normal` | `Special` — trash, workspaces, workspace, directory, file)

**`Graph`**

- `root`, `nodes` map

- derived: `parentByChild`, `ownerParentByChild` (from nodes + child lists, not stored separately in SQL)

**`Change` / `Op`** (`History.fs`)

- `NewNode`, `SetText`, `SetClasses`, `Replace(parent, index, oldChildren, newChildren)`

- `Change` has `id`, `changeId` (Guid for dedup), `ops`

**Client view layer** (`ViewModel.fs`, not the server graph): site tree, selection span, line rendering — see View section below.

## Operations

### Low-level ops (shared client + server)

- [x] create node (`NewNode`)

- [x] set text old/new (`SetText`)

- [x] set CSS classes (`SetClasses`)

- [x] replace children at index (`Replace` — parent-child and ref edges)

- [x] undo/redo via `History` + inverted ops (client submits inverses; server stores forward log)

### Model building

- [x] paste / import text → ops (`Paste.fs`, `ImportText.fs`)

- [ ] bulk create-from-outline helpers (beyond paste/import)

### Site/composite model (client)

type sitenode (conceptual)

- node + occurrence scope

- opened (include children)

- children : nodeview list

root : sitenode; selection : nodeview + span

### High-level ops (derived in client)

- structural delete with promotion, trash (`ViewModelDeleteOps.fs`)

- paste, move, search-driven navigation

- wikilink / `[[filepath]]` handling (desktop hints + import)

## View

- viewroot → nodeview + trace

- lines: editable, key capture, recursive sitenodes respecting fold state

- incremental line updates on site node replace/remove/insert

## Storage (server)

**PostgreSQL** is always the source of truth. Startup initializes the schema and loads graph state from the DB only; correlated files under `DataDir` are not read to rebuild state.

| Layer | Role |
|-------|------|
| **PostgreSQL** | Authority: `changes` append-only log, `graph` singleton (root + revision), `nodes` + `node_children` relational projection |
| **On-disk artifacts** | Projection keyed to document roots (workspace, directory, file nodes); written after each accepted DB commit |

**PostgreSQL** (normalized projection; no outline blob as source of truth):

- `changes` — append-only log (`payload` = full change JSON)

- `graph` — singleton row (root id + revision)

- `nodes`, `node_children` — relational mirror of `Node` / child edges

**On-disk** (under `DataDir`, default `data/` locally, `/home/data` on Azure):

- document artifacts per graph node — outline or payload text under `DataDir/{label}/...` (see [[doc/roadmap/workspace-file-persistence.md]])

- tab-indented outline syntax via `Snapshot.fs` for serialization; not the SQL source of truth

Requires `DB_CONNECTION_STRING`. After each accepted change, the server commits to PostgreSQL and auto-persists affected document artifacts.

Full schema and rules: [[doc/current/persistence-model.md]]. Operations / environments: [[doc/reference/postgres-environments.md]].

Implementation: `Database.fs`, `DbAgent.fs`, `AgentHandle` in `Api.fs`. Legacy `FileAgent.fs` / `Persistence:Mode` rollback hooks remain in code pending removal.

## Testing plan

Goal: TDD where valuable; keep tests fast and layered.

Workflow: smallest failing test → minimal implementation → refactor.

Bias:

- Prefer pure functions in Shared (ops, ViewModel planners, serialization)

- Server: test `DbAgent` / store logic with `TEST_DB_CONNECTION_STRING` when Postgres available

- Avoid browser automation until it pays off

### Domain/ops unit tests

- `applyOp` / `Change.apply` / undo invariants (Shared.Tests)

- Graph invariants after op batches (child refs exist, root exists, ownership rules)

Tooling: **xUnit** in `tests/Shared.Tests` and `tests/Server.Tests`.

### Serialization tests

- JSON round-trip for `Op`, `Change`, `Graph`, API DTOs (`SerializationTests.fs`)

### Persistence tests

- DB: `DbAgentTests.fs` against real Postgres when `TEST_DB_CONNECTION_STRING` is set

- Legacy file: snapshot + log replay (`FileAgent` / document loader tests — rollback path only)

- Replay: load persisted state → apply changes → matches expected graph/revision

### Server tests

- Command handlers behind endpoints (revision increment, change append, conflict behavior when added)

- Optional later: in-memory ASP.NET Core integration tests

### Client tests

- Pure MVU/update helpers where extracted; no Playwright in baseline

## Documentation map

| Doc | Role |
|-----|------|
| [[doc/arch.md]] | Architecture, layers, persistence |
| [[doc/api.md]] | HTTP contract (implemented + target) |
| [[doc/current/sync-mvp.md]] | Current sync semantics (LWW, poll + changes) |
| [[doc/current/persistence-model.md]] | DB schema, correlated files, auto-persist |
| [[doc/current/workspace-graph.md]] | Workspace special nodes and graph invariants |
| [[doc/current/workspace-local-mapping.md]] | Desktop workspace label → local root config |
| [[doc/current/desktop-local-files.md]] | Desktop proxy and `/_desktop/*` API |
| [[doc/reference/postgres-environments.md]] | Dev/prod Postgres setup |
| [[doc/roadmap/postgres-roadmap.md]] | Roadmap index (Postgres, sync, desktop) |
| [[doc/roadmap/workspace-file-sync.md]] | Partial Upload / Download (WebDAV + server git) |
| [[doc/roadmap/workspace-webdav.md]] | Server WebDAV Class 1 mount; PROPFIND datestamps |

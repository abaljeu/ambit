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

data/              file-mode documents (outline, .meta, .log) — local dev default

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

When running in the desktop shell, the client talks to `localhost` (local proxy). Graph authority remains the cloud server; desktop adds `/_desktop/*` for capabilities and file import.

## Desktop

Optional host for users who need local filesystem access while using the same web UI.

- **UI**: WPF `WebView2` loads the local proxy URL (not the cloud origin directly)

- **Proxy** (`src/Desktop/LocalProxy.fs`): forwards `/ambit/*` (and static assets) to the configured cloud base URL; handles `/_desktop/*` locally

- **Capabilities** (`src/Shared/DesktopCapabilities.fs`): `GET /_desktop/capabilities` — what file open/import/export the host allows

- **Import**: `POST /_desktop/import` reads a local path and returns ops/text for the client to apply via normal cloud sync

- **Export**: `POST /_desktop/export` writes tab-indented child text from the client to a local file (not directories)

- **Auth**: desktop can store cloud session cookie (`AuthStore.fs`) so the proxied app is authenticated

The desktop host does **not** become a second source of truth for the graph. See [[doc/future/overview.md]] §7 for product intent.

## Server

The server:

- Serves `GET /ambit` (HTML shell from `gambol.template.html`) and Fable bundles from `wwwroot`

- Exposes JSON API under `/ambit` (state, poll, changes)

- Persists graph + append-only change log via **file** and/or **PostgreSQL** (see Storage)

- Optional cookie auth (`Auth:Username` / `Auth:Password` in config → derived token cookie)

Key modules: `Api.fs` (`AgentHandle`), `FileAgent.fs`, `DbAgent.fs`, `Database.fs`, `DatabaseSetup.fs`, `ChangeLog.fs`, `DocumentLoader.fs`.

### Sync (multi-client, N<5)

Assumption: multiple clients (up to 5) may operate on the same model concurrently.

**Current baseline** (see [[sync-mvp]]): last-write-wins by arrival order on the server.

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

Deferred vs older docs: `POST /undo`, `POST /redo`, `GET /ops?since=…` as separate endpoints — not exposed; undo is client-side.

## Domain model

A pure, directed, potentially cyclic graph (`src/Shared/Model.fs`).

**`Node`**

- `id` : `NodeId` (`Guid`)

- `text`, `name` (`string option`)

- `children` : `ChildNode list` — each child has `ref: Ownership` (`Owner` | `Ref`) and `id`

- `cssClasses`, `owner`, `kind` (`Normal` | `Special` e.g. trash)

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

Two explicit **persistence modes** (`Persistence:Mode` in config; default **`db`**):

| Mode | Authority | Startup | Notes |

|------|-----------|---------|--------|

| **`db`** | PostgreSQL | Schema init; load from DB only — **no** silent import from files | Requires `DB_CONNECTION_STRING` |

| **`file`** | On-disk outline + `.meta` + `.log` | `FileAgent` loads snapshot, replays log | Optional DB mirror when connection string set |

**File artifacts** (under `DataDir`, default `data/` locally, `/home/data` on Azure):

- outline snapshot (tab-indented text; `Snapshot.fs`)

- `.meta` — server revision after snapshot + replay

- `.log` — one JSON `Change` per line (same payload concept as SQL `changes`)

**PostgreSQL** (normalized projection; no outline blob as source of truth):

- `changes` — append-only log (`payload` = full change JSON)

- `graph` — singleton row (root id + revision)

- `nodes`, `node_children` — relational mirror of `Node` / child edges

`db` mode periodically writes **disk backup** from DB state (export only, not read at startup). `file` mode may seed an empty DB from file state on startup.

Full schema and mode rules: [[doc/future/persistence-vs-domain-model.md]]. Operations / environments: [[doc/future/postgres-migration.md]], [[doc/future/postgres-environments.md]].

Implementation: `Database.fs`, `DbAgent.fs`, `FileAgent.fs`, `AgentHandle` in `Api.fs`.

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

- File: snapshot + log replay (`FileAgent` / document loader tests)

- DB: `DbAgentTests.fs` against real Postgres when `TEST_DB_CONNECTION_STRING` is set

- Replay: load persisted state → apply changes → matches expected graph/revision

### Server tests

- Command handlers behind endpoints (revision increment, change append, conflict behavior when added)

- Optional later: in-memory ASP.NET Core integration tests

### Client tests

- Pure MVU/update helpers where extracted; no Playwright in baseline

## Documentation map

| Doc | Role |
|-----|------|
| [[doc/arch.md]] | Architecture, layers, persistence modes |
| [[doc/api.md]] | HTTP contract (implemented + target) |
| [[doc/sync-mvp.md]] | Current sync semantics (LWW, poll + changes) |
| [[doc/future/persistence-vs-domain-model.md]] | DB schema vs domain model |
| [[doc/future/postgres-environments.md]] | Dev/prod Postgres setup |

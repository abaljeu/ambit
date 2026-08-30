# Roadmap Overview

Summary of committed architectural directions for Gambol, distilled from the roadmap documents in this folder. Each section identifies the high-level decision and the document(s) that elaborate it.

[x] = every aspect of this item is competed.
Action: Find the next step in the process, propose the action to the user.  Mark off completed actions with [x].  Update linked documents to keep in-sync with status.  This file is the roadmap index. Implemented baselines live in [[doc/current/]]. Linked documents may require updating.
A record of actions taken shall be maintained in [[doc/legacy/Commands executed into Azure.txt]].

## 0. PostgreSQL as the persistence back-end

**Status:** [x] Implemented — see [[doc/current/persistence-model.md]].

**Decision:** PostgreSQL with a normalized relational schema mirroring the domain model (`Node`, child edges, `Ownership`), not outline file syntax.

- [x] Dual-write / file mirror period completed.
- [x] No outline blobs in SQL.
- [x] Change log parity with `gambol.log` in file mode.
- [x] DB presence status on client status line.

*Sources:* [[doc/current/persistence-model.md]], [[doc/reference/postgres-environments.md]].

- 
---
## 1. Establish a PostgreSQL server

**Status:** [x] Implemented — see [[doc/reference/postgres-environments.md]].

- [x] Azure Flexible Server provisioned, `gambol` database, App Service network access.
- [x] `DB_CONNECTION_STRING` in production.
- [x] Start/stop scripts in `scripts/`.

*Source:* [[doc/reference/postgres-environments.md]] (sections 3, 8, 9).



---

## 2. Drop file authority when DB is present

**Status:** [x] Implemented — see [[doc/current/persistence-model.md]].

`db` / `file` mode split is live. Details: server implementation and mode rules in
[[doc/current/persistence-model.md]]; sync semantics in [[doc/current/sync-mvp.md]].

*Sources:* [[doc/current/persistence-model.md]], [[doc/roadmap/future-merge-sync.md]].

---

## 3. Server-authoritative merge

**Decision:** The database server is the sole merge authority. No other actor (client, local webserver) performs merge. The client submits changes against whatever base it has; the server rebases them against the current head and appends the result. This is the right split because PostgreSQL holds the complete model, while clients and the server warm cache may hold only partial views ([[on-demand-graph-residency]]).

**Key sub-decisions:**

- **Smart rebase / orphan rescue:** The rebase stub currently discards changes that fail to apply. Instead, when a change targets a node that no longer exists, the server walks up the ownership chain to find the nearest surviving ancestor and applies the change there.
- **Rebase-style convergence:** A lagging client submits a batch built against an earlier revision. The server interprets that batch against the *current* head, not the stale base. The merge/rebase logic lives in `Shared/` (so it compiles for both server and tests) but runs only on the server in production.
- **Edit wins over delete:** When a delete and a concurrent edit collide, the edit survives. This is encoded explicitly in merge rules.
- **Conflict marker nodes:** Merge is a complete computation. When automation cannot resolve a conflict it *creates* conflict-marker nodes for the user to clear manually.
- **Not real-time collaboration:** Simultaneous editing is treated as a safety net for rare overlap, not as a sub-second shared-cursor experience.
- **No client-side rebase:** The client does not attempt to rebase stale changes. It submits them as-is and accepts whatever the server returns (success with the merged result, or rejection).

*Source:* [[doc/roadmap/future-merge-sync.md]].

> **Note:** [[doc/legacy/robust-client-server-sync.md]] describes a client-side rebase/409 protocol. That design is superseded by server-side merge: the server accepts stale-base submissions directly, so 409 conflict responses and client rebase are not needed.

---

## 4. Robust client-server sync

**Decision:** The client maintains a local pending-change queue with auto-retry and persistence, so edits survive network failures and tab closes.

**Key sub-decisions:**

- **Auto-retry with exponential backoff:** Failed POSTs are retried automatically (2 s → 4 → 8 → … → 60 s cap).
- **localStorage queue persistence:** The pending queue is saved to `localStorage` so uncommitted changes survive page reloads.
- **UUID-tagged changes:** Each `Change` carries a `changeId: Guid` for server-side deduplication (retry → OK).
- **Server handles stale bases:** The client submits changes with their original base revision. The server rebases and merges (see section 3). The client does not need 409/rebase logic.
- **Accepting the server result:** The client submits its entire pending queue in one batch. The server merges all of it and returns the canonical result. The client replaces its local graph with that result; the pending queue is now empty.
- **Polling for remote changes:** The client polls `GET /ambit/changes-since/{revision}` on focus, on `window.online`, and on a ~15-second interval. Remote changes are applied immediately unless they touch a dirty edit, in which case they are deferred until the edit commits or cancels.
- **IndexedDB / offline cache:** Whether to add a client-side persistent cache (IndexedDB) for offline support is open; the current plan assumes low-latency server access and uses simple browser state.

*Source:* [[doc/legacy/robust-client-server-sync.md]] (auto-retry and localStorage portions; client-rebase portions are superseded by server-side merge).

---

## 5. Client-side memory management (document-level loading)

**Decision:** Introduce *document* as a first-class concept in the app. **Today** the whole graph is one document (monolithic snapshot). **Target:** one graph, many documents with on-demand residency. Prior to document roots in the workspace model, the app has no explicit document partition — only the undifferentiated graph.

**What a document is:**

There is always exactly one graph in PostgreSQL. A *document* is a named partition of that graph: a **document root** (root NodeId as identity unless a later requirement demands a separate ID) assigns membership to every node in that partition (Owner-tree ancestry from the root; Ref edges do not confer membership). The graph is not split into separate stores; document membership is a property of each node within the single graph.

In the workspace file model, document roots are `Special Workspace`, `Directory`, and `File` nodes (including implicit ROOT). See [[doc/roadmap/workspace-file-model.md]] § Documents. Persistence, client load/unload, and replication all use the same document boundary. Per-document server persistence is implemented; **on-demand graph residency** adds membership metadata, scoped loaders, and client/server load/unload — authority: [[on-demand-graph-residency]].

**How it begins:**

- **Now:** one document — the entire graph. All nodes belong to it; `GET /state` bootstraps the full graph ([[doc/current/sync-mvp]]).
- **Target:** documents rooted at Workspace/Directory/File Special nodes; load/unload at document granularity. A loaded document has its complete node payloads and child lists resident. Outside the loaded set, retain only boundary headers and document descriptors — **not** global topology.

**Why this boundary:**

Payloads and complete child lists are the memory pressure. The document is the natural unit for deciding what to hold — coarse enough to be manageable, fine enough to match user intent about what they are working on. Navigation across unloaded documents goes through descriptors, boundary headers, and on-demand fetch (`NeedsDocuments`), not a fully resident edge map.

**Key sub-decisions:**

- **Hybrid search:** Instant search over loaded document payloads for local results; async server-side search for results across unloaded documents ([[on-demand-graph-residency]]).
- **Topology is not globally resident (supersedes prior commitment):** Do not keep all edges for all documents in memory. Unloaded documents contribute boundary headers and descriptors only; required closures load on touch.

*Sources:* [[on-demand-graph-residency]], [[doc/legacy/memory-management.md]] (historical; topology-always-resident decision superseded).

---

## 6. Replication unit: whole documents

**Decision:** The unit of replication between server and client is a *whole document* — the full set of nodes under a document root within the single server graph, stopping at nested document roots (boundary headers only). Edits are node-level, but sync and caching deal in complete documents. Cross-document references are allowed; cross-document edits are logged as a single operation with enough payload for per-document projections to update independently. Conflict checking targets per-document base versions while a global change sequence remains for audit and catch-up.

Implementation workset: [[on-demand-graph-residency]] (supersedes the older “document load units” framing that kept topology fully resident).

*Sources:* [[on-demand-graph-residency]], [[doc/roadmap/future-merge-sync.md]].

## 7. Desktop app with local webserver

**Status:** [~] Partially implemented — see [[doc/current/desktop-local-files.md]].

**Decision:** Desktop host with local HTTP proxy; cloud remains graph authority.

**Implemented:**

- WPF WebView2 + `LocalProxy` forwarding `/ambit/*` to cloud.
- `/_desktop/*` for capabilities, file-status, import (`GET /file`), export (`POST /file`).
- Import and Export client commands; file-reference indicator on active row.
- Workspace path resolution via local mapping — [[doc/current/workspace-local-mapping.md]].

**Not implemented:**

- Open file or workspace in system explorer (`open` capability).
- Startup workspace registration (sync local config labels to cloud graph).
- Full workspace filesystem API (dir/file CRUD with `modifiedUtc` conflicts).

**Server workspace files:** Documents rooted at workspace, directory, and file nodes persist on the server under `{DataDir}/{label}/{path}`. Desktop label mapping and manual Import/Export are unchanged. See [[doc/roadmap/workspace-file-model.md]] § Documents and server file persistence.

*Source:* [[doc/current/desktop-local-files.md]], [[doc/current/workspace-stage-plan.md]].

# Roadmap Overview

Summary of committed architectural directions for Gambol, distilled from the roadmap documents in this folder. Each section identifies the high-level decision and the document(s) that elaborate it.

[x] = every aspect of this item is competed.
Action: Find the next step in the process, propose the action to the user.  Mark off completed actions with [x].  Update linked documents to keep in-sync with status.  overview.md is current information. Linked documents may require updating.
A record of actions taken shall be maintained in [[doc/legacy/Commands executed into Azure.txt]].

## 0. PostgreSQL as the persistence back-end


**Decision:** Move from flat-file-only storage to PostgreSQL with a normalized relational schema that mirrors the domain model (`Node`, child edges, `Ownership`), not the outline file syntax.

**Key sub-decisions:**

- [x] **File authority (dual-write period):** During migration the on-disk snapshot + `.meta` + `.log` files remain the source of truth. The database is a *projection*. The `nodes` and `graph` tables are updated in step with every file write — not derived by replaying the `changes` log. At startup the file-derived `Graph` is compared to the DB; on mismatch the DB is rebuilt from files. The goal during this period is zero mismatch errors; any mismatch indicates a bug in the write path that must be fixed before proceeding to Step 2.
- [x] **No outline blobs in SQL:** The line-oriented snapshot format lives only in `Snapshot.fs`. The SQL schema stores domain records: `nodes`, `node_children`, a singleton `graph` row, and an append-only `changes` table.
- [x] **Change log parity:** Each row in `changes` corresponds to one line in `gambol.log`, keeping the two audit trails aligned.

- [x] Add DB presence status next to sync on the status line.  Init at load.

*Sources:* [[doc/roadmap/persistence-vs-domain-model.md]] (canonical schema spec), [[doc/roadmap/postgres-migration.md]] (operational summary), [[doc/roadmap/database-migration.md]] (Azure setup notes), [[doc/roadmap/postgres-environments.md]] (dev-to-prod environment management).

- 
---
## 1. Establish a PostgreSQL server

**Decision:** Before migration phases, provision a production PostgreSQL host and wire the app to it via `DB_CONNECTION_STRING`.

**Pre-step scope:**

- [x] Provision **Azure Database for PostgreSQL - Flexible Server** (PostgreSQL 17, smallest burstable tier).
- [x] Create the `gambol` database and credentials.
- [x] Configure network access from Azure App Service (`Amble`) to the database.
- [x]  Set `DB_CONNECTION_STRING` in Azure App Service environment variables.
- [x] Define a script in `scripts/` to turn the Azure-hosted PostgreSQL server on/off for clock savings.

- Deploy once so startup runs `initSchema` and initial rebuild/parity checks.

This pre-step does not change merge/sync architecture. It only establishes the database host so section 1 can run in dual-write mode.

**Cost pressure:** Provisioning the database starts a financial clock (~$13–25/month on Azure Burstable). To manage this during low-usage periods, implement a start/stop automation so the server is only running when in use. If Azure costs remain unfavorable within the first month, evaluate switching to DigitalOcean Managed PostgreSQL (~$15/month, simpler pricing). The app is provider-neutral — only the connection string and provisioning scripts change.

*Source:* [[doc/roadmap/postgres-environments.md]] (Decision now, section 3 setup, section 8 checklist, section 9 cost management).



---

## 2. Drop file authority when DB is present

**Decision:** Make the database the single source of truth when it is available at startup. If
the database is absent at startup, fall back to file mode for that process. Keep an explicit
`file` mode for rollback and local file-authority operation.

**Status:** Implemented as the current `db` / `file` mode split. In `db` mode, a working
startup database is authoritative and maintains a periodic file-format backup from DB state.
If the database is absent at startup, the server uses file authority for that run. Once DB
authority has started, later DB write failures fail change requests rather than switching mode.
`file` keeps the old file-authority rollback path.

**What changes:**

- In `db` mode with a working startup DB, file writes (`gambol`, `gambol.meta`, `gambol.log`) are removed from the mutation path. API state lives in the `nodes`, `node_children`, `graph`, and `changes` tables.
- Startup file-vs-DB comparison is removed for DB authority. A present DB is trusted on startup; an absent DB falls back to file mode for that process.
- Backup and disaster-recovery procedures shift from file authority to database backup, with a new file-format backup refinement: periodically write snapshot text, `.meta`, an empty `.log`, and the existing snapshot backup rotation from DB state only.
- `file` mode remains available as an explicit rollback/local mode: files are authoritative, an empty DB may be seeded from files, and successful file writes mirror to DB when DB is available.

**What stays the same:**

- The schema, write logic, and `changes` table from Step 1 are unchanged.
- The `changes` table remains the append-only audit trail from which any past state can be reconstructed.
- The server API is unchanged — clients see no difference.

*Sources:* [[doc/roadmap/persistence-vs-domain-model.md]], [[doc/roadmap/future-merge-sync.md]].

---

## 3. Server-authoritative merge

**Decision:** The database server is the sole merge authority. No other actor (client, local webserver) performs merge. The client submits changes against whatever base revision it has; the server rebases them against the current head and appends the result. This is the right split because the server always has the full graph, while the client may hold only a partial view.

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

**Decision:** Introduce *document* as a first-class concept in the app. Prior to this step, the app has no document concept — there is one undifferentiated graph.

**What a document is:**

There is always exactly one graph on the server. A *document* is a named partition of that graph: any node can be designated as a document root by fiat, which assigns it a new `docId`. Every node owned by that root belongs to the same document. The graph is not split into separate stores; document membership is a property of each node within the single graph.

**How it begins:**

- Initially there is one document: the entire graph. All nodes belong to it.
- New documents are created by a split operation: designating a node as a document root generates a new `docId` and assigns that `docId` to all nodes owned by it.
- Load/unload decisions operate at document granularity. A loaded document has all its node payloads resident. An unloaded document is absent from the payload map; only its topology edges remain so the graph structure is intact for navigation.

**Why this boundary:**

Topology (edges) is small enough to keep fully in memory across all documents. Payloads (node text and content) are the memory pressure. The document is the natural unit for deciding what payloads to hold — coarse enough to be manageable, fine enough to match user intent about what they are working on.

**Key sub-decisions:**

- **Hybrid search:** Instant search over loaded document payloads for local results; async server-side full-text search for global results across unloaded documents.
- **Topology stays fully in memory:** Edge structure for all documents is always resident. Navigation and path-finding are always synchronous.

*Source:* [[doc/legacy/memory-management.md]].

---

## 6. Replication unit: whole documents

**Decision:** The unit of replication between server and client is a *whole document* — the full set of nodes with a given `docId` within the single server graph. Edits are node-level, but sync and caching deal in complete documents. Cross-document references are allowed; cross-document edits are logged as a single operation with enough payload for per-document projections to update independently.

*Source:* [[doc/roadmap/future-merge-sync.md]].

## 7. Desktop app with local webserver

**Decision:** Ship a desktop application that runs a local webserver and presents the same browser-based UI. The cloud server remains the authority for all graph data. The local webserver acts as a transparent proxy — the client's base URL points to `localhost`, and all graph queries and mutations are forwarded to the cloud. The local webserver additionally handles requests for local files, which the cloud server cannot reach.

**What "local files" means:**

Local files are user content on the machine's filesystem — outline text, Markdown, images, PDFs, or other attachments. This is unrelated to the flat-file storage from Steps 0–2 (which is gone by this point). Nodes can carry a `[[filepath]]` wikilink to a path on disk. **Import** reads that local file (or directory listing) and replaces the focused node's children with the parsed outline; mutations still go through the normal client pending queue and cloud `/changes` endpoint. **Export** and **open** (launch a file with its default application) remain future commands. Attachments referenced from nodes may eventually be served from the local filesystem.

**Import and sync (current direction):**

- The desktop host reads files; it does not become a second source of truth for the graph.
- Until nodes carry reliable dates, any existing local file or folder is treated as newer than the cloud copy for UI hints only; timestamp-based stale detection is future work.
- Richer tagging (which nodes came from which source file, round-trip export) is future work; the import response shape leaves room for it without new node metadata yet.
- Whole-document load/unload (sections 5–6) is orthogonal: import is node-scoped today, not "create a new document."

**Technology:**

A .NET desktop host with WebView2 and a small local HTTP proxy (`/_desktop/...`) in front of the cloud API.

**User experience (target):**

The user launches the desktop app. It opens a browser-like window on the local proxy, which forwards to the cloud. The UI matches the web client, with desktop-only affordances when capabilities allow: a visible process working directory, a left-margin hint on the active row for the first `[[filepath]]` reference, and **Import file** when import is enabled. Export and open follow later.

*Source:* Not yet elaborated in a dedicated document.

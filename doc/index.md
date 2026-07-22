# Project Index

Category: Project coordination
See Also: [[doc/README.md]], [[doc/arch.md]], [[doc/spec.md]], [[doc/api.md]]

This is the global index for what exists, what is implemented, and how active development plans are sequenced. It should stay short: link to authoritative docs instead of repeating their details.

## Current Features

Implemented baselines in [[doc/current/]]. One detail source per feature.

### **Product and architecture**
Details: [[doc/arch.md]].
Summary: Client/server MVU app, graph ops model, project structure, and layer boundaries.

### **API contract**
Details: [[doc/api.md]].
Summary: Implemented and target HTTP endpoints for `/ambit` and related surfaces.

### **Multi-client sync**
Details: [[doc/current/sync-mvp.md]].
Summary: Change batches, acked change IDs, polling, and last-write-wins server authority.

### **Persistence (PostgreSQL + correlated files)**
Details: [[doc/current/persistence-model.md]].
Summary: PostgreSQL is always authoritative; on-disk document artifacts correlate with graph nodes and auto-persist from DB edits.

### **Workspace graph**
Details: [[doc/current/workspace-graph.md]].
Summary: Workspace, directory, and file special nodes, placement invariants, and ref context.

### **Desktop local files**
Details: [[doc/current/desktop-local-files.md]].
Summary: WebView2 proxy, `/_desktop/*` capabilities, import/export, and file-status.

### **Workspace local mapping**
Details: [[doc/current/workspace-local-mapping.md]].
Summary: Desktop config mapping workspace labels to absolute local filesystem roots.

## Development Sequence

Active and upcoming work only. Completed baselines live under **Current Features** above.

Status terms: **Partial** means some behavior is current and some remains planned. **Planned** means committed direction exists but no current baseline is listed here. **Evolving** means design or semantics are still being shaped.

### **Server-authoritative sync and merge**
Status: **Partial**.
Details: [[doc/current/sync-mvp.md]], [[doc/roadmap/future-merge-sync.md]].
Last implemented: The running baseline uses change batches, acked change IDs, polling, and server revision checks without client-side merge.
Might be next: Server-side merge/rebase for stale submissions, including orphan rescue and explicit conflict-marker nodes.

### **Desktop local files and workspace mapping**
Status: **Partial**.
Details: [[doc/current/desktop-local-files.md]], [[doc/current/workspace-stage-plan.md]].
Last implemented: Desktop proxy capabilities cover local import, export, local file status, and `//label/relative` workspace path resolution; workspace namespace file-status now queries server `DataDir`.
Might be next: Local mapping command surface or richer freshness metadata.

### **Workspace file model and persistence**
Status: **Partial**.
Details: [[doc/roadmap/workspace-file-model.md]], [[doc/current/workspace-stage-plan.md]].
Last implemented: Per-document snapshot integration, incremental persist, server file-status, and unresolved-reference UI on top of `DataDir` live-save and unified path moves.
Might be next: XML read/write ([[doc/roadmap/workspace-format-xml.md]]); expand-to-parse and richer freshness metadata/UI.

### **Lazy Load and workspace source formats**
Status: **Partial**.
Details: [[doc/roadmap/lazy-load.md]], [[doc/roadmap/workspace-scale-import.md]].
Last implemented: Disk-to-graph reconciliation for added, deleted, renamed/moved, and modified source paths under the named Workspace (historically after server receive; target trigger is WebDAV push + finish-commit — [[workspace-file-sync]]). Identity-preserving renames, TRASH/ref semantics, exact `.amb` handling, `M` → Unparsed, graph-only persistence, idempotency, and best-effort failure policy are covered.
Might be next: Reconcile after WebDAV push+commit; expand-to-parse and richer freshness metadata/UI (Lazy Load step 3).

### **Workspace file sync (WebDAV + server git)**
Status: **Planned**.
Details: [[workspace-file-sync]], [[workspace-webdav]], [[lazy-load]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]].
Direction: Map / Push / Pull over WebDAV Class 1 under `/ambit/dav/{label}/…`. Push: local scope → check-ignore (mapped tree) → PUT/MKCOL → finish-commit. Pull: server PROPFIND inventory → check-ignore (DataDir SoT on server) → GET. PROPFIND exposes getlastmodified ([[workspace-webdav]]). Not git remotes / pack transport.
Might be next: Shared scope helpers, server WebDAV with DataDir-filtered PROPFIND + finish-commit, desktop Push walk / Pull GET, Map/Push/Pull commands; Lazy Load reconcile after push+commit.

### **Amble run**
Status: **Evolving**.
Details: [[doc/roadmap/amble-run.md]].
Last implemented: The active slice defines `AmbleRun.run` as the orchestration point from focus-line parse/eval to graph ops.
Might be next: Client Run command wiring and later slices that derive graph ops from evaluated node lists.

## Currency Rules

- If an item is fully implemented, its durable behavior should be in [[doc/current/]] or [[doc/reference/]], not only in [[doc/roadmap/]].
- If this index contradicts a current doc, the current doc wins and this index should be corrected.
- If two current docs disagree, surface the contradiction for clarification before updating this index.

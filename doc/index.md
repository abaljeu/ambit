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
Last implemented: Desktop proxy capabilities cover local import, export, file status, and `//label/relative` workspace path resolution; workspace structure commands are implemented through Stage 8.
Might be next: Stage 5 unresolved UI and server file-status corrections.

### **Workspace file model and persistence**
Status: **Partial**.
Details: [[doc/roadmap/workspace-file-model.md]], [[doc/current/workspace-stage-plan.md]].
Last implemented: Stage 8 snapshot integration and incremental persist on top of Stage 7 `DataDir` live-save and unified path moves.
Might be next: XML read/write slice (Stage 7 Step 6 — [[doc/roadmap/workspace-format-xml.md]]); workspace scale import slice 1; Stage 5 unresolved UI corrections.

### **Workspace import and source formats**
Status: **Planned**.
Details: [[doc/roadmap/workspace-scale-import.md]].
Last implemented: No current baseline is listed here.
Might be next: Repo file-tree browsing with on-demand parse/edit for individual files.

### **Git workspace sync (desktop pull/push via gateway)**
Status: **In progress** (G0–G6 desktop done; G7 client connect/commands next).
Details: [[git-sync-gateway]], [[workspace-scale-import-slice2-plan]].
Last implemented: Desktop folder picker + workspace-mappings Get/Put; G5 git ops + gateway + PAT already in place.
Depends on: Workspace scale import slice 1 (Stage 7 `DataDir` live-save is done).
Might be next: G7 Connect/Clone/Pull/Push client commands + sync status.

### **Amble run**
Status: **Evolving**.
Details: [[doc/roadmap/amble-run.md]].
Last implemented: The active slice defines `AmbleRun.run` as the orchestration point from focus-line parse/eval to graph ops.
Might be next: Client Run command wiring and later slices that derive graph ops from evaluated node lists.

## Currency Rules

- If an item is fully implemented, its durable behavior should be in [[doc/current/]] or [[doc/reference/]], not only in [[doc/roadmap/]].
- If this index contradicts a current doc, the current doc wins and this index should be corrected.
- If two current docs disagree, surface the contradiction for clarification before updating this index.

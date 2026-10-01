# Feature Index

Category: Project coordination
See Also: [[doc/README.md]], [[doc/current/arch.md]], [[doc/spec.md]], [[doc/api.md]], [[plan/roadmap/map.md]]

Index of the current program. One detail source per feature. What to work on next is [[plan/roadmap/map.md]].

## Current Features

Implemented baselines in [[doc/current/]].

### **Product and architecture**
Details: [[doc/current/arch.md]].
Summary: Architecture wiki home. The first Reference view is the project structure.

### **Browser**
Details: [[doc/current/browser.md]].
Summary: Fable MVU loop, outline render, selection, and client-local undo.

### **Server**
Details: [[doc/current/server.md]].
Summary: `/ambit` HTTP, persistence agents, and the as-built Core mailbox. Core behavior: [Server Core](plan/architecture/server-core.md).

### **Operations**
Details: [[doc/current/operations.md]].
Summary: Shared ops that transform the graph. Node and Graph fields stay on [[doc/current/persistence-model.md]].

### **View**
Details: [[doc/current/view.md]].
Summary: Browser site tree, selection span, and line rendering.

### **Testing**
Details: [[doc/current/testing.md]].
Summary: xUnit layers for Shared ops, serialization, persistence, and server handlers.

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
Summary: Workspace, directory, and file special nodes, placement invariants, DocumentState plus ParseState/PersistState axes, and ref context.

### **Desktop local files**
Details: [[doc/current/desktop-local-files.md]].
Summary: WebView2 proxy, `/_desktop/*` capabilities, import/export, and file-status.

### **Workspace local mapping**
Details: [[doc/current/workspace-local-mapping.md]].
Summary: Desktop config mapping workspace labels to absolute local filesystem roots.

### **Workspace file sync**
Details: [[doc/current/workspace-file-sync.md]].
Summary: WebDAV Upload / Download, ignore inventory, prepare-push / finish-commit, download manager, and sync ledger.

## Currency Rules

- This file describes the current program only. Planned work lives in `plan` Projects; the goto is [[plan/roadmap/map.md]].
- If an item is fully implemented, its durable behavior should be in [[doc/current/]] or [[doc/reference/]], not only in [[doc/roadmap/]].
- If this index contradicts a current doc, the current doc wins and this index should be corrected.
- If two current docs disagree, surface the contradiction for clarification before updating this index.

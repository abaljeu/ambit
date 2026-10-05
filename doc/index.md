# Feature Index

Category: Project coordination
See Also: [[doc/README.md]], [[doc/current/architecture.md]], [[doc/spec.md]], [[doc/current/api.md]], [[plan/roadmap/map.md]]

Index of the current program. One detail source per feature. What to work on next is [[plan/roadmap/map.md]].

## Current Features

Implemented baselines in [[doc/current/]].

### **Product and architecture**
Details: [[doc/current/architecture.md]].
Summary: The architecture wiki home links building blocks, capabilities, information, and contracts, and project structure is the first view.

### **Browser**
Details: [[doc/current/browser.md]].
Summary: Fable MVU loop, outline render, selection, and client-local undo.

### **Server**
Details: [[doc/current/server.md]].
Summary: `/ambit` HTTP, cookie auth, and the change log. Core is [[doc/current/core.md]].

### **Core**
Details: [[doc/current/core.md]].
Summary: Server subsystem. The hub links the mailbox, parse and persist, the file agent, the db agent, and Actors.

### **Operations**
Details: [[doc/current/operations.md]].
Summary: Shared ops that transform the graph. Cases live on [[doc/current/op.md]]. Node and Graph fields stay on [[doc/current/persistence-model.md]].

### **Op**
Details: [[doc/current/op.md]].
Summary: One Graph modification. Cases, apply, and which fields an Op writes.

### **View**
Details: [[doc/current/view.md]].
Summary: Browser site tree, selection span, and line rendering.

### **Testing**
Details: [[doc/current/testing.md]].
Summary: xUnit layers for Shared ops, serialization, persistence, and server handlers.

### **API**
Details: [[doc/current/api.md]].
Summary: Index of published contracts between parties.

### **HTTP contract**
Details: [[doc/current/http-contract.md]].
Summary: Browser and Server JSON for `/ambit` state, poll, changes, load, and git save.

### **AI agent protocol**
Details: [[doc/current/ai-agent-protocol.md]].
Summary: Ack-only wake POST and `POST /ambit/actors/deliver` for the external Grok bot.

### **Multi-client sync**
Details: [[doc/current/sync-mvp.md]].
Summary: Last-write-wins server authority. Event post, poll, and state live on the HTTP contract.

### **Persistence (PostgreSQL + correlated files)**
Details: [[doc/current/persistence-model.md]].
Summary: The database keeps graph and event info. A start with the database offline reads file data into a partial graph. That partial graph is not used for editing.

### **Workspace graph**
Details: [[doc/current/graph.md]].
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

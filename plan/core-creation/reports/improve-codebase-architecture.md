# Improve codebase architecture — Core hot spot

Date: 2026-09-06

[[.agents/skills/improve-codebase-architecture/SKILL.md]] review. No product edits. Hot spot from recent commits and the dirty tree: Server Core and the HTTP Adapter. Browser is a secondary check. Vocabulary: module, interface, depth, seam, adapter, leverage, locality ([[.agents/skills/codebase-design/SKILL.md]]); domain names from [[CONTEXT.md]]. HTML was written only to the OS temp directory.

Spec: [[../project.md]], [[../map.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], issues 03–24.

## Top recommendation

[[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]]. Highest leverage on the current hot spot. Stop unpacking Core at HTTP. Keep Committed Decision 0003 (no Core-level `postChange` facade). Bound Changes becomes the test surface. Files and Query wait.

## Candidates

### 1. Bind Changes at the Core seam — Strong · in-process

Files: [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/Core/CoreCredentials.fs]], [[src/Server/Core/CoreRuntime.fs]].

Problem: the HTTP Adapter dismantles Core into `changes()`, credentials, and `browserCredential`, then runs `CoreAuth.post` itself. The interface is nearly the admission implementation. Leakage across the seam.

Solution: bind Changes inside Core (same pattern as `parseBound`). The Adapter posts through one nested interface.

Wins: locality of admission in Core; leverage of one nested call; interface shrinks and auth hides; tests hit bound Changes.

Aligns with [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]: keep nesting; do not add a Core-level `postChange` facade.

[[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]] is Status `done`: production posts present a live Credential; HTTP holds `CoreRuntime` as Core. Leftover: `/ambit/changes` still unpacks three Core pieces into [[src/Server/Api.fs]] `postChange`. Parse already uses `CoreAuth.bindHandle`. This candidate is that leftover. Ticket: [[../issues/25-bind-changes-at-core-seam.md|25]].

### 2. Give Core a Files module — Worth exploring · ports & adapters

Files: [[src/Server/Core/CoreRuntime.fs]], [[src/Server/SavePrep.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/Api.fs]].

Problem: Core API names Files, but `flushFileSnapshot` and `getFileRevision` hang on the container. Callers bounce Changes + CoreRuntime stubs + `dataDir`. Deletion test: deleting the stubs moves the same three closures; they do not vanish. Current stubs are shallow.

Solution: Files as a Core subobject. File and db adapters sit at that seam. SavePrep talks only to Files.

Wins: depth as Files absorbs stubs; two adapters justify the seam; locality of open/write in one module; Core API becomes the test surface.

Today’s container-level flush mildly contradicts Committed Decision 0003. A Files subobject restores the container shape.

Do not start here. This is [[../issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]].

### 3. Move Poll and Load behind Query — Worth exploring · in-process

Files: [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/Core/CoreChanges.fs]].

Problem: Sync and Load composition live in the HTTP Adapter (`getPoll`, `postLoad`, `getState`). Core exposes primitives. Query is missing. Deletion test: deleting `Api.getPoll` / `postLoad` does not vanish the composition; it reappears in RouteRegistration or Shared.

Solution: deepen Query (or Changes) so Poll and Fetch compose behind one interface. HTTP stays a thin Adapter.

Wins: leverage of one Query call; locality of Sync composition; Adapter stops owning Load; tests skip HTTP JSON.

Do not start here. This is [[../issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]].

### 4. Collapse three Change enqueue paths — Worth exploring · local-substitutable

Files: [[src/Server/Api.fs]], [[src/Server/GraphOnlyChangePost.fs]], [[src/Server/LazyLoadReconciliationServer.fs]], [[src/Server/Core/CoreChanges.fs]].

Problem: understanding “post a Change” requires bouncing three Adapters. `GraphOnlyChangePost` is a shallow chunk loop past the Core object. Browser vs Parse vs LazyLoad differ mainly by which post function and which Credential.

Solution: one Core enqueue interface; chunking becomes an internal seam. Callers differ only by Credential.

Wins: locality of one enqueue module; delete two shallow posts; the interface is the test surface; leverage across Parse and Sync.

Related to 25, not swallowed by it. Do not collapse Graph-only chunking in 25.

### 5. One Browser Change Adapter — Speculative · ports & adapters

Files: [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/App.fs]].

Problem: workspace bootstrap bypasses the SyncPlanner seam and posts Changes on a second Adapter (`applyAndPostSync` vs `Effect.SubmitPendingBatch`).

Solution: keep sync ordering, but send every Change through the same port Adapter as ordinary edits.

Not this Project. Browser Sync is outside the Core increment.

## Mapping

| Candidate | Strength | Existing issue |
| --- | --- | --- |
| Bind Changes at the Core seam | Strong | [[../issues/25-bind-changes-at-core-seam.md|25]] (leftover after [[../issues/23-close-core-object-seam.md|23]]) |
| Files module | Worth exploring | [[../issues/07-define-core-files-contract.md|07]] |
| Poll and Load behind Query | Worth exploring | [[../issues/08-define-core-query-contract.md|08]] |
| Three Change enqueue paths | Worth exploring | none; related to 25 |
| Browser Change Adapter | Speculative | not this Project |

## Deletion test notes

- `CoreAuth` earns keep. Admission must sit inside bound Changes, not in the Adapter.
- `GraphOnlyChangePost` earns keep only if two or more chunk callers share one Core helper.
- `CoreRuntime` earns keep for agent selection. Many tests still construct FileAgent / DbAgent and skip the Core object ([[../issues/05-place-core-changes-in-existing-projects.md|05]] allows tests). Production policy is under-exercised if CoreRuntime is not the test surface.

## Not this review

Did not start [[../issues/17-cancel-a-job.md|17 (Cancel a job)]]. Did not start [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Did not grill [[../issues/07-define-core-files-contract.md|07]] or [[../issues/08-define-core-query-contract.md|08]]. Did not propose a Core-level `postChange` facade.

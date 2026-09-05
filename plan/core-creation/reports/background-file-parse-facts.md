# Background file Parse facts

Date: 2026-09-05

Purpose: Design-support inventory for a future Core task-manager use case: Parse is on demand today; files that are not current should be parsed in the background. This report records repo evidence only. It does not choose product decisions or edit plans. Related: [[CONTEXT.md]], [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[doc/roadmap/lazy-load.md]], [[.agents/skills/codebase-design/SKILL.md]].

## Deep Module frame

**Parse** is an Actor **definition** outside Core: it reads file bytes (via Files get or upload body), plans Ops on a Graph snapshot, and concludes through **Core Changes** Graph-only inner apply. **Core** owns the Actor pool **Implementation** behind **Command** (launch) and **Changes** (output admission). A background Parse sweep is not a fifth Core API call; it is one or many Parse Actor jobs launched through **Command**, with Browsers learning results through **Poll** ([[plan/core-creation/reports/kernel-fsproj.md]], [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]]).

## 1. What "file not current" means today

Several distinct states appear in code and docs. They overlap in UI but have different canonical types and functions.

| State | Canonical type / function | Meaning |
| --- | --- | --- |
| **Unparsed File Node** | `DocumentState.Unparsed` in [[src/Shared/Model.fs]]; set by `LazyLoadReconciliationApply.markUnparsed`, `WorkspaceUploadStructure.markUnparsed`, `DocumentAssembly.seedUnparsedStub` | Server artifact exists; Graph content does not represent parsed file text. File Node may have empty `children` (stub). Blocks content edits via `DocumentPartition.isMemberOfInaccessibleDocument` ([[src/Shared/DocumentPartition.fs]], [[src/Shared/History.fs]]). |
| **NoServerFile** | `DocumentState.NoServerFile` | File Node exists; no server body yet. Desktop Upload creates stubs here; `ImportDocument.planParseFile` rejects with `"no file on server"`. |
| **Current but stale on disk** | Git `M` on an ordinary file → `LazyLoadReconciliationApply.markUnparsed` (`Current` → `Unparsed`) without reading content ([[src/Shared/dotnet/LazyLoadReconciliation.fs]] `invalidateOrParseModified`) | Graph structure may remain; `documentState` marks content invalid until Parse. |
| **Current warm reconcile** | `DocumentState.Current` + `ImportDocument.planParseFile` with `previousArtifactText` from `DocumentFormat.writeArtifact` | Graph is Current but disk text may differ; Parse warm-reconciles via `DocumentParseOps.planApplyArtifact` / `DocumentWarm.readArtifact` ([[doc/roadmap/parse-file-reconcile-current.md]]). |
| **Graph stamp vs disk mtime** | `Node.updateTime` vs `File.GetLastWriteTimeUtc`; `DocumentPersistence.planParseFile` may append `Op.SetUpdateTime` when they differ | After Parse, graph metadata aligns to artifact mtime. Not a separate enum; used in persist and download stamp alignment ([[src/Shared/WorkspaceUploadStructure.fs]] `planAlignFileStampOps`). |
| **Desktop/server path freshness** | `WorkspacePathSyncStatus` (`NoServerFile`, `NewerOnServer`, `NewerOnDesktop`, `Synced`, `Unparsed`, …) in [[src/Shared/WorkspacePathSyncStatus.fs]]; resolved in [[src/Shared/ViewModelRowState.fs]] | Client-side ledger comparison when desktop is mapped. `Unparsed` overlays `Synced` when `documentState = Unparsed`. Distinct from server authoritative `DocumentState`. |
| **Unloaded vs Loaded children** | `ChildrenStatus` in [[src/Shared/Model.fs]]; hollow bullet when `Unloaded` or `Unparsed` ([[src/Shared/ViewModelChildrenIndicator.fs]]) | Residency / Fetch concern, not parse currentness. An Unparsed File Node is typically Loaded with empty children or Unloaded stub. |
| **Future inferred currentness** | [[plan/event-sourced-ops/details/vocabulary.md]] documents intent to remove `DocumentState` and infer unparsed from file vs File Node dates | Not implemented; field remains authoritative today ([[doc/current/workspace-graph.md]]). |

**Directory File** (exact `.amb`): same `DocumentState` on the owning Directory or Workspace Node. Modified `.amb` may be parsed inline during lazy-load reconciliation via `LazyLoadReconciliationApply.parseDirInfoIfPresent` when artifact text is read ([[src/Shared/dotnet/LazyLoadReconciliationApply.fs]]). Ordinary File Nodes are not parsed during structural reconciliation.

## 2. Discovery and on-demand Parse triggers today

| Trigger | Discovery | Parse behavior |
| --- | --- | --- |
| **User Load / Parse command** | User focus → `CommandEntry` `ParseFile fileId` ([[doc/roadmap/parse-file-reconcile-current.md]]) | Client `UpdateImport.parseFileOp` → `Effect.ContinueParseFile` → `POST /ambit/file/parse` ([[src/Client/UpdateImport.fs]], [[src/Server/Api.fs]] `postParseFile`). |
| **Upload push (desktop)** | After WebDAV push, `WorkspaceUploadStructure.planServerFilePresentOps` marks `NoServerFile` → `Unparsed` for uploaded/skipped paths | Optional immediate Parse of focused file; else `ContinueDirectoryReconcile` ([[src/Client/UpdateWorkspaceSync.fs]]). |
| **Upload batch reparse** | `WorkspaceUploadStructure.shouldReparseAfterMtimeSkip` — only `Unparsed` files after mtime skip | `Effect.ContinueUploadParses` queues sequential Parse requests ([[src/Shared/ViewModelSync.fs]], [[src/Client/App.fs]]). |
| **Lazy-load / git reconciliation** | `WorkspaceGit.changedPathsBetween` → `LazyLoadReconciliation.ChangedPath` list ([[src/Server/WorkspaceGit.fs]]); or filesystem discovery via `DocumentPersistence.discoverArtifactRelatives` under a scope ([[src/Server/LazyLoadReconciliationServer.fs]]) | Structural ops only for most paths; `Modified` marks **Unparsed**; Added `.amb` may parse via `parseDirInfoInfos`. Does **not** parse ordinary File bodies. |
| **Git push post-receive** | `GitGateway.completeWorkspacePush` → `changedPathsBetween` → `LazyLoadReconciliationServer.reconcileChangedPaths` ([[src/Server/GitGateway.fs]], [[src/Server/RouteRegistration.fs]]) | Same structural pipeline as above. |
| **Manual / repair reconcile** | `POST /ambit/workspace/reconciliation/directory` or `/added` with workspace + optional path | `reconcileChangedPathsWithDiscovery` unions git delta with disk discovery ([[src/Server/LazyLoadReconciliationServer.fs]]). |
| **Cold bootstrap** | `DocumentPersistence.readAllDocuments` / `discoverArtifactRelatives` — all files under `DataDir` | Directory Files parsed into outline; other files become Unparsed stubs ([[src/Server/DocumentPersistence.fs]] comment ~777). |
| **WebDAV finish-commit** | `WorkspaceWebDav.handleFinish` commits git only; **no** reconciliation hook in Server code today ([[src/Server/WorkspaceWebDav.fs]]) | [[doc/roadmap/lazy-load.md]] lists finish-commit as target trigger; client may call directory reconcile separately. |

**On-demand Parse path (single File):** `handle.getState` → `DocumentPersistence.planParseFile` (optional DataDir write if body text) → `ImportDocument.planParseFile` → `DocumentParseOps.planApplyArtifact` → one `Change` via `handle.postGraphOnlyChange` ([[src/Server/Api.fs]]). Request **awaits** apply; ack is bare `{ok:true}`.

## 3. Enumerable work set vs incremental discovery

**No complete server-side enumerable "all Unparsed files" work set exists today.**

- Closest scan: `DocumentPersistence.enumerateDocumentRoots` walks all document roots in the Graph for **persist**, not Parse scheduling ([[src/Server/DocumentPersistence.fs]]).
- Lazy-load discovery is **scoped and incremental**: git path delta, optional directory tree walk under `DataDir/{label}` or `DataDir/{label}/{dirRel}`, unioned in `reconcileChangedPathsWithDiscovery`.
- Client Upload may queue multiple Parse effects (`ContinueUploadParses`) but only for paths known from that upload batch.
- Roadmap "expand-to-parse" is one File at a time on user expand ([[doc/roadmap/lazy-load.md]] § Planned next work), not a full-workspace sweep.

A background Parse Actor must either **define discovery** (Query + Files scan, workspace walk, or incremental cursor) or **consume an external work list** (Command argument, git delta follow-up). Discovery itself is not a settled Core **Interface** fact.

## 4. Future background Parse Actor shape (evidence-bound)

Existing docs and code constrain a deep Module behind **Command** + **Changes**:

| Concern | Evidence |
| --- | --- |
| **Launch context** | **Command** selects Actor definition and returns Core-owned job identity ([[plan/core-creation/issues/09-define-core-command-launch-contract.md]], [[plan/core-creation/reports/kernel-fsproj.md]] §Uploaded file). Parse definition stays outside Core; pool inside. |
| **File selection** | Per-file: `fileId` + optional text ([[src/Server/Api.fs]]). Per-scope: lazy-load changed paths or discovery ([[src/Server/LazyLoadReconciliationServer.fs]]). No typed "all unparsed" selector yet. |
| **Bounded planning** | `DocumentParseLimits.refuseText` — 50k UTF-16 code units ([[src/Shared/DocumentParseLimits.fs]]). Planning runs off apply queue in assessment ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). |
| **Typed Graph-only Change output** | `ImportDocument.planParseFile` → `Op list`; Graph-only path reserved for Parse ([[plan/core-creation/issues/03-define-typed-core-changes-contract.md]]). Unparsed adds leading `Op.SetDocumentState(Unparsed, Current)`. |
| **Chunking** | Large op lists split at `GraphOnlyChangeChunks.maxOps = 80`; `GraphOnlyChangePost.postChunks` posts sequential Changes, revision +1 per success ([[src/Shared/GraphOnlyChangeChunks.fs]], [[src/Server/GraphOnlyChangePost.fs]]). Parse POST today uses **one** Change, not chunks. |
| **Revalidation before apply** | Today: Parse embeds `change.id = stateResponse.revision` (revision CAS). Assessment: amend-on-success instead ([[plan/event-sourced-ops/details/actors-and-jobs.md]] §Parse File, [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]). |
| **Cancellation** | Open: issue [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]]; assessment recommends no cancel-after-enqueue ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). No `CancellationToken` in Server today. |
| **Retries / failures** | Parse planning returns `Result<Op list, string>`. Apply batch is all-or-nothing Reject per Change ([[plan/core-creation/issues/03-define-typed-core-changes-contract.md]]). Lazy-load reconcile failures are logged best-effort; sync response unchanged ([[doc/roadmap/lazy-load.md]]). |
| **Poll visibility** | Each admitted Change bumps **Revision**; Browsers **Poll** tail ([[CONTEXT.md]], [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]] §Progress gap). Job percent / staged Parse not specified. Optional **Query** job snapshot is open ([[plan/core-creation/issues/08-define-core-query-contract.md]]). |

Recommended sequence after Upload ([[plan/core-creation/reports/kernel-fsproj.md]]): Files send → Changes (Unparsed stub) → **Command** launch Parse → Parse reads Files get → inner apply. Background sweep would add a discovery step before or inside Command launch.

## 5. Concurrency hazards

| Hazard | Evidence |
| --- | --- |
| **File bytes change during Parse** | `planParseFile` reads text at plan time; optional `writeArtifactText` on upload body. No re-read before apply. Concurrent disk write could make applied ops stale vs bytes. |
| **Graph Changes during planning** | Parse uses snapshot at `getState`; revision CAS today can refuse if Browser edits land before apply ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). Target: amend as newest Actor. |
| **Duplicate Parse** | No lock or dedup for "Parse file X already running." Two concurrent `POST /ambit/file/parse` for same `fileId` could plan from same revision and race on apply. Soft-lock is accepted direction but not implemented ([[plan/event-sourced-ops/details/soft-lock.md]], ESO issue 09). |
| **Ordering** | Apply mailbox serializes Changes. Multi-chunk lazy-load posts sequential Changes with updated revision ([[src/Server/GraphOnlyChangePost.fs]]). Actor assessment: Server arrival sequences concurrent Actors ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). |
| **Partial chunk commit** | `postChunks` stops on first Error; prior chunks remain applied — not atomic across chunks ([[plan/core-creation/reports/initial-core-changes-runtime-facts.md]] gap 6). Large background Parse would inherit this unless finish packaging locks one batch ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]). |
| **Starvation / fairness** | Single apply mailbox; no priority spec ([[plan/core-creation/issues/02-core-actor-pool.md]]). Background sweep could block Browser edits at admission (Direction 4 in [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]]). |
| **Unparsed invariant vs structural reconcile** | Already-Unparsed document rejects structural reconciliation ([[doc/roadmap/lazy-load.md]] §Unparsed operation invariant). Background Parse must complete `SetDocumentState` → Current ordering before content Replace ops ([[src/Shared/History.fs]]). |
| **Timeout abandon** | `FileAgent.runBounded` 8000 ms can abandon persist Task that may still write ([[src/Server/FileAgent.fs]], [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]]). Long Parse planning on-queue vs off-queue is unresolved. |

## 6. Project ownership and gap vs issues 03–06

| Owner | Scope |
| --- | --- |
| [[plan/core-creation/project.md]] | Core Changes seam (03–06 delivered/planned), issue 01 produce path, issue 02 Actor pool, issues 07–12 Command/cancel/shutdown grilling |
| [[plan/event-sourced-ops/project.md]] | Parse realignment tracer (issue 08), Browser job access / soft-lock (issue 09), merge/consume behavior |
| [[doc/roadmap/lazy-load.md]] / workspace-scale roadmap | expand-to-parse, freshness UI, disk-to-graph reconciliation (structural, not File body Parse) |
| [[plan/roadmap/epics/chapters/actors-supported.md]] | Parse as first Actor definition using pool |
| [[plan/roadmap/epics/chapters/incremental-operations.md]] | Incremental Upload/Load (separate from pool machinery) |
| [[plan/roadmap/epics/chapters/acid-apply.md]] | Timeout replacement, file view-only mode, persist authority |

**Inside initial Core Changes increment (issues 03–06):** typed `GraphAgentHandle`, Normal and Graph-only post paths, HTTP Adapter boundary, sole-writer intent. Parse stays HTTP/on-demand; no Command, pool, or background sweep.

**Outside 03–06 (required for background Parse):** issue 01 inner apply for producers; issue 02 pool; issues 09–12 Command launch, cancel, finish, shutdown; ESO 08 Parse realignment; optional ESO 09 soft-lock for duplicate-Parse policy; lazy-load expand-to-parse and freshness metadata ([[doc/roadmap/workspaces-checklist.md]] unchecked items); no issue today for "enumerate and Parse all non-current files at startup or idle."

## 7. Smallest useful design questions (not decisions)

1. **Work-set source:** Should background Parse discover candidates from authoritative Graph Query (`documentState = Unparsed`), from Files/git delta, or from an explicit Command payload list?
2. **Granularity:** One Core job per File Node, one job per workspace sweep, or a scheduler job that spawns per-file jobs?
3. **Current vs Unparsed only:** Should background Parse include warm reconcile of `Current` files when disk mtime or git `M` already marked them Unparsed, or also proactively warm-reconcile `Current` nodes?
4. **Directory File scope:** Are `.amb` Directory Files in scope for the same background Actor as ordinary File Nodes, given inline parse already exists in lazy-load reconciliation?
5. **Trigger coupling:** Is background Parse tied to startup, post-reconcile git delta, idle timer, or explicit Command only — and does it replace or supplement user `Ctrl+Shift+>` Parse?
6. **Chunk atomicity:** For large files, is partial multi-Change progress acceptable to Browsers (Poll per chunk), or must one file's Parse be one atomic Change or rollback-safe batch?
7. **Interaction with expand-to-parse:** Does fold/expand Parse remain user-driven while background Parse handles the rest, or does one subsume the other?
8. **Progress Interface:** Is job visibility Poll-only (count of Revisions), or does **Query** expose job state ([[plan/core-creation/issues/08-define-core-query-contract.md]])?

## Summary

Today "not current" is primarily `DocumentState.Unparsed` (plus client `WorkspacePathSyncStatus` and separate `ChildrenStatus` residency). Discovery is incremental (git diff, scoped disk walk, user focus); no full-workspace Unparsed enumerator exists. Parse is synchronous HTTP per file; lazy-load reconciliation marks files Unparsed but parses only Directory Files inline. A background Parse Actor fits the assessed Command + pool + Graph-only Changes shape, with chunking, amend, cancel, and discovery all still open outside issues 03–06.

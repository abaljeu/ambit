# Work Board

Live actionable work only. Empty sections mean nothing is known pending there. Git history is the audit trail; completed items are deleted, not archived.

## Legend

Each entry is one actionable item: a link to the durable source or target, a concise expected outcome, and optional owner or blocker detail.

Entry format:

```
- [[path/to/artifact]] — expected outcome (owner: root-agent-id)
```

Mutations for delegated workers to return to their parent: `add`, `move`, `block`, `remove`.

## Active

Work currently being executed.

- [[.scratch/owner-edge-db-repair/spec.md]] — extend startup sweep: ACID repair of `node_children` Owned tree (GC unreachable; promote Ref when reachable node has no owner) (artifacts: [[.scratch/owner-edge-db-repair/implement.md]], [[src/Shared/ProjectionOwnershipRepair.fs]])
- [[.scratch/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]] — keep Current when Load Workspace rediscovers Added path; demote only new stubs / NoServerFile (plan: fix_load_demotes_parse_8d40752b; artifacts: [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]], [[tests/Shared.Tests/LazyLoadReconciliationTests.fs]])

## Pending

Work ready to start but not yet claimed.

- [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md]] — next wayfinder frontier: Top-level context Node versus text (parent: [[.scratch/expression-language/map.md]])
- [[.scratch/client-start-time/issues/09-cache-first-boot-delayed-lcp.md]] — HITL: `/ambit` bundle LCP of `div.amb-text` back near ~1 s (artifacts: [[.scratch/client-start-time/reports/cache-first-boot-delayed-lcp.md]])
- [[.scratch/client-start-time/issues/08-poll-hash-fallback-loop.md]] — HITL: one `/state` then Poll confirms; Selection must not jump to ROOT (artifacts: [[.scratch/client-start-time/reports/poll-hash-fallback-loop.md]])
- [[.scratch/client-start-time/reports/cold-load-loading-hang.md]] — HITL re-verify cold load past Loading..., then warm F5 cache hit (parent: [[.scratch/client-start-time/reports/implement-cache-first-boot-01-07.md]])
- [[.scratch/client-start-time/reports/reload-state-reuse-investigation.md]] — server revision-keyed bootstrap encode cache for warm F5 at unchanged revision (artifacts: [[src/Server/Api.fs]], [[src/Shared/ResidentProjection.fs]]; parent: [[.scratch/client-start-time/reports/state-further-optimization.md]])
- [[.scratch/client-start-time/reports/bucket-3-post-state-work.md]] — defer `applyFoldSession` to after first paint; first render collapsed SiteMap only (artifacts: [[src/Client/App.fs]], [[src/Client/SessionState.fs]]); lower priority after HITL: restore 8ms / 18 rows ([[.scratch/client-start-time/reports/production-hitl-after-deploy.md]])
- [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]] — HITL F5: Load Workspace, focus a sub-node (no Zoom), refresh; owning Workspace Loaded and zoom stays at prior zoomRoot / in-ROOT (not zoomed into selection) (artifacts: [[src/Shared/ResidentProjection.fs]] sessionTargets, [[src/Client/SessionState.fs]])
- [[.scratch/selective-client-loading/issues/21-load-one-selected-target-through-synchronization.md]] — HITL verify Load of Unloaded named Workspace after stub-skip fix (inventory → push → `/load` with packages; no `/changes` name conflict) (artifacts: [[src/Shared/WorkspaceUploadStructure.fs]], [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]])
- [[tmp/load-performance-audit.md]] — secondary: ensure ledger reuse on already-synced Load (Mask path); diagnose empty-ledger resets (artifacts: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] needsSeed, [[src/Shared/dotnet/WorkspaceFileSync.fs]] ensureLedgerSeeded)
- [[tmp/load-performance-audit.md]] — skip workspace-inventory when Unloaded (empty stub path) (artifacts: [[src/Client/UpdateWorkspaceSync.fs]], [[src/Shared/WorkspaceUploadStructure.fs]])
- [[tmp/load-performance-audit.md]] — defer/narrow path-sync ledger waterfall after push (artifacts: [[src/Client/App.fs]] runWorkspacePathSyncSnapshot, [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] liveStatusRows)
- [[.scratch/selective-client-loading/issues/22-load-full-selection.md]] — load same-Workspace multi-target selections with deduplicated Workspace packages, refusing selections that span more than one Workspace (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]] — keep navigation and Find synchronous over resident content (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]] — guard all structural Change plans, including MoveSelected, from Unloaded child lists (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/reports/two-phase-state-loading-exploration.md]] — validate spec-break (folds widen bootstrap), Phase 1 thin-id-list feasibility, production \|V⁺\| vs Workspace; reconcile with cache-first boot; decisions captured, promotion pending validation (parent: [[.scratch/selective-client-loading/spec.md]])
- [[doc/reference/dev-debug-workflow.md]] — document watch: prefer `/ambit?debug=1`; after esbuild rebuild hard-reload (Ack on CodeOutdated does not unblock)
- [[src/Shared/SyncLogic.fs]] — decide whether to ignore page-stamp drift when deploy stamp matches during Fable/esbuild watch
- [[.scratch/glossary-directory-file/rename-isMarker.md]] — optional remaining speech/doc sweep for informal “marker” (Directory File sense); `isMarker` / related API renames done
- [[.scratch/large-node-cursor-perf/delete-children-cost.md]] — profile/optimize delete among large siblings (fromNodes + SiteMap rematch / structural DOM plan) (parent: [[.scratch/large-node-cursor-perf/project.md]])

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

- [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]] — Move dialog does not offer Unloaded destinations (blocked by: [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]])
- [[.scratch/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] — promote implemented client residency while retaining future server residency in the roadmap (blocked by: [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]], [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]], [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]], [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]])
- [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] — dispatch full-selection Load from the hollow-circle control (blocked by: [[.scratch/selective-client-loading/issues/22-load-full-selection.md]], [[.scratch/selective-client-loading/issues/23-introduce-hollow-circle-presentation.md]])

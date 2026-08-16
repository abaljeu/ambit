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

- [[.scratch/selective-client-loading/look-ahead-remaining-slices.md]] — decide whether to start 3b by encoding queued items as `ChangeRequest.Change` (parent: [[.scratch/selective-client-loading/undo-implementation-plan.md]])
- [[.scratch/selective-client-loading/undo-implementation-plan.md]] — implement Slice 3b (wire runtime History and projected local flow) (parent: [[.scratch/selective-client-loading/undo-spec.md]])
- [[tmp/warm-parse-dual-owner-fix.md]] — HITL verify Current warm File Load after reclaim-vs-trash fix; dual-Owner gone (artifacts: [[src/Shared/documents/DocumentColdParse.fs]], [[src/Shared/History.fs]], [[tests/Shared.Tests/ImportDocumentTests.fs]])
- [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]] — HITL F5: Load Workspace, focus a sub-node (no Zoom), refresh; owning Workspace Loaded and zoom stays at prior zoomRoot / in-ROOT (not zoomed into selection) (artifacts: [[src/Shared/ResidentProjection.fs]] sessionTargets, [[src/Client/SessionState.fs]])
- [[.scratch/selective-client-loading/issues/21-load-one-selected-target-through-synchronization.md]] — HITL verify Load of Unloaded named Workspace after stub-skip fix (inventory → push → `/load` with packages; no `/changes` name conflict) (artifacts: [[src/Shared/WorkspaceUploadStructure.fs]], [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]])
- [[tmp/load-performance-audit.md]] — secondary: ensure ledger reuse on already-synced Load (Mask path); diagnose empty-ledger resets (artifacts: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] needsSeed, [[src/Shared/dotnet/WorkspaceFileSync.fs]] ensureLedgerSeeded)
- [[tmp/load-performance-audit.md]] — skip workspace-inventory when Unloaded (empty stub path) (artifacts: [[src/Client/UpdateWorkspaceSync.fs]], [[src/Shared/WorkspaceUploadStructure.fs]])
- [[tmp/load-performance-audit.md]] — defer/narrow path-sync ledger waterfall after push (artifacts: [[src/Client/App.fs]] runWorkspacePathSyncSnapshot, [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] liveStatusRows)
- [[.scratch/selective-client-loading/issues/22-load-full-selection.md]] — load same-Workspace multi-target selections with deduplicated Workspace packages, refusing selections that span more than one Workspace (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]] — keep navigation and Find synchronous over resident content (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]] — guard all structural Change plans, including MoveSelected, from Unloaded child lists (parent: [[.scratch/selective-client-loading/spec.md]])
- [[doc/reference/dev-debug-workflow.md]] — document watch: prefer `/ambit?debug=1`; after esbuild rebuild hard-reload (Ack on CodeOutdated does not unblock)
- [[src/Shared/SyncLogic.fs]] — decide whether to ignore page-stamp drift when deploy stamp matches during Fable/esbuild watch
- [[src/Client/Program.fs]] — optional hardening: `fetchTextNoCacheWithFail` for `/ambit/state` (not the primary hang)
- [[.scratch/glossary-directory-file/rename-isMarker.md]] — optional remaining speech/doc sweep for informal “marker” (Directory File sense); `isMarker` / related API renames done

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

- [[.scratch/node-bullet-tooltip/issues/02-client-bullet-tip-wiring.md]] — HITL verify three Bullet hover tips and unchanged click/fold/zoom (blocked by: browser check; audit: [[.scratch/node-bullet-tooltip/pending-audit-tooltip-hitl.md]])
- [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]] — Move dialog does not offer Unloaded destinations (blocked by: [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]])
- [[.scratch/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] — promote implemented client residency while retaining future server residency in the roadmap (blocked by: [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]], [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]], [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]], [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]])
- [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] — dispatch full-selection Load from the hollow-circle control (blocked by: [[.scratch/selective-client-loading/issues/22-load-full-selection.md]], [[.scratch/selective-client-loading/issues/23-introduce-hollow-circle-presentation.md]])

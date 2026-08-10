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

## Pending

Work ready to start but not yet claimed.

- [[src/Client/UpdatePaste.fs]] — HITL verify external multiline Ctrl+V (select + edit): siblings kept; lines become nodes (fix: [[src/Shared/documents/DocumentColdParse.fs]] planPasteOps; tests: [[tests/Shared.Tests/DocumentColdParseTests.fs]])
- [[doc/roadmap/workspace-file-sync.md]] — HITL verify auto-download on persist: edit a mapped-workspace file (own edit + remote poll) refreshes the local mapped folder with no feedback loop; plain web is a no-op (plan: auto-download-persisted-files_560c6923; artifacts: [[src/Shared/WorkspaceSyncScope.fs]], [[src/Shared/WorkspaceUploadStructure.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/Update.fs]], [[src/Client/App.fs]])
- [[.scratch/node-bullet-tooltip/issues/02-client-bullet-tip-wiring.md]] — HITL verify: hover a chevron, solid-circle, and hollow-circle Bullet each show the tip; click/fold/zoom unchanged (implemented, Shared suite green; artifacts: [[src/Shared/ViewModelRowState.fs]] bulletTip, [[src/Client/RowView.fs]], [[src/Client/JsInterop.fs]])
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

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

- [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]] — Move dialog does not offer Unloaded destinations (blocked by: [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]])
- [[.scratch/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] — promote implemented client residency while retaining future server residency in the roadmap (blocked by: [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]], [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]], [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]], [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]])
- [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] — dispatch full-selection Load from the hollow-circle control (blocked by: [[.scratch/selective-client-loading/issues/22-load-full-selection.md]], [[.scratch/selective-client-loading/issues/23-introduce-hollow-circle-presentation.md]])

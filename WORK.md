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

- [[.scratch/selective-client-loading/issues/16-rename-upload-to-load.md]] — rename Upload to Load without changing source synchronization behavior (parent: [[.scratch/selective-client-loading/spec.md]])
- [[.scratch/selective-client-loading/issues/17-represent-unloaded-child-lists-end-to-end.md]] — represent unloaded child lists while preserving canonical owner identity (parent: [[.scratch/selective-client-loading/spec.md]])
- [[doc/current/workspace-graph.md]] — add SYSTEM to ROOT's fixed children and canonical special-node table
- [[doc/reference/dev-debug-workflow.md]] — document watch: prefer `/ambit?debug=1`; after esbuild rebuild hard-reload (Ack on CodeOutdated does not unblock)
- [[src/Shared/SyncLogic.fs]] — decide whether to ignore page-stamp drift when deploy stamp matches during Fable/esbuild watch
- [[src/Client/Program.fs]] — optional hardening: `fetchTextNoCacheWithFail` for `/ambit/state` (not the primary hang)

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

- [[.scratch/selective-client-loading/issues/18-synchronize-a-resident-projection-safely.md]] — reconcile and apply materialized Change tails to a resident projection while clearing local History (blocked by: [[.scratch/selective-client-loading/issues/15-introduce-history-action-messaging.md]], [[.scratch/selective-client-loading/issues/17-represent-unloaded-child-lists-end-to-end.md]])
- [[.scratch/selective-client-loading/issues/19-bootstrap-fresh-sessions-with-complete-root.md]] — bootstrap complete ROOT without sending the full graph (blocked by: [[.scratch/selective-client-loading/issues/18-synchronize-a-resident-projection-safely.md]])
- [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]] — restore at most one saved zoom Workspace during bootstrap (blocked by: [[.scratch/selective-client-loading/issues/19-bootstrap-fresh-sessions-with-complete-root.md]])
- [[.scratch/selective-client-loading/issues/21-load-one-selected-target-through-synchronization.md]] — load one selected target through serialized synchronization (blocked by: [[.scratch/selective-client-loading/issues/16-rename-upload-to-load.md]], [[.scratch/selective-client-loading/issues/19-bootstrap-fresh-sessions-with-complete-root.md]])
- [[.scratch/selective-client-loading/issues/22-load-full-selection-across-workspaces.md]] — load mixed selections with deduplicated Workspace packages (blocked by: [[.scratch/selective-client-loading/issues/21-load-one-selected-target-through-synchronization.md]])
- [[.scratch/selective-client-loading/issues/23-make-hollow-circle-clicks-invoke-load.md]] — dispatch full-selection Load from the hollow-circle affordance (blocked by: [[.scratch/selective-client-loading/issues/22-load-full-selection-across-workspaces.md]])
- [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]] — keep navigation and Find synchronous over resident content (blocked by: [[.scratch/selective-client-loading/issues/19-bootstrap-fresh-sessions-with-complete-root.md]])
- [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]] — guard ordinary structural commands from unloaded child lists (blocked by: [[.scratch/selective-client-loading/issues/17-represent-unloaded-child-lists-end-to-end.md]])
- [[.scratch/selective-client-loading/issues/26-move-selected-content-into-an-unloaded-destination.md]] — preserve projected MoveSelected disappearance and Undo/Redo parity (blocked by: [[.scratch/selective-client-loading/issues/18-synchronize-a-resident-projection-safely.md]], [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]])
- [[.scratch/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] — promote implemented client residency while retaining future server residency in the roadmap (blocked by: [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]], [[.scratch/selective-client-loading/issues/23-make-hollow-circle-clicks-invoke-load.md]], [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]], [[.scratch/selective-client-loading/issues/26-move-selected-content-into-an-unloaded-destination.md]])

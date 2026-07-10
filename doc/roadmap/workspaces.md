# Workspaces

Category: Workspace scale
See also: [[doc/index]], [[doc/README]]

Current documents remain authoritative for implemented behavior. This index organizes workspace roadmap direction and preserved history; it does not promote abandoned implementation or historical plan status to current truth.

## Current baselines

- [[doc/current/workspace-graph]]
- [[doc/current/workspace-local-mapping]]
- [[doc/current/workspace-stage-plan]]
- [[doc/current/desktop-local-files]]
- [[doc/current/persistence-model]]

## Active roadmaps

- [[doc/roadmap/workspace-name-verbatim]] — drop `@` disk marker; `//name` references (no `@name:`)
- [[doc/roadmap/workspace-name-immutable]] — rename refuses for workspace nodes
- [[doc/roadmap/workspace-file-model]]
- [[doc/roadmap/workspace-file-persistence]]
- [[doc/roadmap/workspace-scale-file-and-db-management]]
- [[doc/roadmap/workspace-scale-import]]
- [[doc/roadmap/workspace-scale-import-slice1-plan]]
- [[doc/roadmap/workspace-scale-import-slice2-plan]]
- [[doc/roadmap/git-sync-gateway]]
- [[doc/roadmap/revising-workspace-file-model]]
- Formats: [[doc/roadmap/workspace-format-amb]], [[doc/roadmap/workspace-format-md]], [[doc/roadmap/workspace-format-plain]], [[doc/roadmap/workspace-format-code]], [[doc/roadmap/workspace-format-xml]], [[doc/roadmap/workspace-format-dispatch]]
- Conversion: [[doc/roadmap/workspace-text-outline-conversion]]

## Retained planned decisions

The following are retained direction only and are not currently implemented after the restart:

- Named workspace folders use verbatim DataDir/{workspaceLabel} names. Code does not add or strip an `@` disk marker; references use `//name`, not `@name:`.

Implementation plan: [[doc/roadmap/workspace-name-verbatim]]. Slice A + B done; immutable names and Filename `@` charset remain out of scope.

- Workspace names are immutable after creation; rename refuses for `Special Workspace` nodes.

Implementation plan: [[doc/roadmap/workspace-name-immutable]]. Remains Planned until Shared rename guards and tests land. Git/desktop remapping is out of scope for that plan.

## Restart boundary

The workspaces branch starts at 35f2976. The db branch ending at 22e28ca remains reference material. Do not replay 5a24a88..22e28ca; selectively reimplement reviewed decisions in small verified changes.

## Discarded-range document inventory

The range 35f2976..22e28ca changed 20 documentation paths.

### New plans to carry

- [[doc/roadmap/workspace-scale-import-slice1-plan]] — carry with Planned status and without claims that discarded implementation is current.
- [[doc/roadmap/workspace-scale-import-slice2-plan]] — carry with Planned status and proposed endpoints and flows clearly marked as such.

### Roadmap edits to reconcile or carry

- [[doc/roadmap/workspace-scale-import]]
- [[doc/roadmap/workspace-scale-file-and-db-management]]
- [[doc/roadmap/git-sync-gateway]]
- [[doc/roadmap/workspace-file-model]]
- [[doc/roadmap/revising-workspace-file-model]]
- [[doc/roadmap/workspace-file-persistence]]
- [[doc/roadmap/workspace-format-plain]]
- [[doc/roadmap/workspace-text-outline-conversion]]
- [[doc/roadmap/postgres-roadmap]]

Reconcile these against the restart baseline before carrying claims about path layout, ownership, parse behavior, remote naming, or implementation status.

### Current and index edits reset to truthful pre-implementation status

- [[doc/index]]
- [[doc/arch]]
- [[doc/current/workspace-graph]]
- [[doc/current/workspace-stage-plan]]
- [[doc/current/persistence-model]]
- [[doc/current/workspace-local-mapping]]

The restart versions remain authoritative. Any useful clarification from the discarded range must be rechecked against implementation before being reapplied.

### Accidental empty files excluded

- git-sync-gateway.md.md
- reference-expressions.md.md
- workspace-scale-import.md.md

These accidental twins are not copied or linked.

## Preserved Cursor plans

Raw copies live under doc/history/workspaces/plans. They are historical evidence, not authoritative plans: they conflict with one another and retain stale todo statuses.

### Antecedent model plans

- [[doc/history/workspaces/plans/stage_3_code_map_bc797199.plan]]
- [[doc/history/workspaces/plans/stage_3_code_map_303f4fae.plan]]
- [[doc/history/workspaces/plans/file-owned-special-nodes_a2a18418.plan]]

### Slice 1 evolution

- [[doc/history/workspaces/plans/slice_1_file_lifecycle_2cccd547.plan]]
- [[doc/history/workspaces/plans/simplify_slice1_ownership_452f7ccc.plan]]
- [[doc/history/workspaces/plans/simplify_slice1_ownership_ffc8a965.plan]]
- [[doc/history/workspaces/plans/special-placement-reconcile_0f939c00.plan]]
- [[doc/history/workspaces/plans/workspace-scale-slice1_77295ba6.plan]]
- [[doc/history/workspaces/plans/slice_1_simplified_ae292de6.plan]]
- [[doc/history/workspaces/plans/slice_1_simplified_c97b7f48.plan]]

### Retained decisions

- [[doc/history/workspaces/plans/drop_@_marker_from_workspace_disk_paths_492fd207.plan]]
- [[doc/history/workspaces/plans/lock_workspace_name_immutable_ea821a05.plan]]

### Slice 2

- [[doc/history/workspaces/plans/slice_2_git_sync_475f1bd7.plan]]
- [[doc/history/workspaces/plans/slice_2_sync_semantics_3c172a96.plan]]

### Parse revision

- [[doc/history/workspaces/plans/expand_to_parse_49607dcd.plan]]

### Restart

- [[doc/history/workspaces/plans/restart_workspace_branch_1da1b902.plan]]

## Carryover sequence

Create a named git stash containing this preservation bundle, create workspaces at 35f2976, apply the named stash, then reconcile roadmap statuses and current documents before implementation begins.

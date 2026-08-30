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

- [[.scratch/daily-git-save/project.md]] — once-per-UTC-day background `commitAll` after listen; git subprocess only (no DbAgent wait) (artifacts: [[.scratch/daily-git-save/reports/implement.md]], [[src/Server/DailyGitSave.fs]])
- [[.scratch/client-start-time/reports/edit-indent-old-text-mismatch.md]] — verify/fix Editing `returnTo` + `adjustModeAfterServerApply` vs Poll/Load under edit+indent Tab CAS
- [[.scratch/owner-edge-db-repair/spec.md]] — extend startup sweep: ACID repair of `node_children` Owned tree (GC unreachable; promote Ref when reachable node has no owner) (artifacts: [[.scratch/owner-edge-db-repair/implement.md]], [[src/Shared/ProjectionOwnershipRepair.fs]])
- [[.scratch/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]] — keep Current when Load Workspace rediscovers Added path; demote only new stubs / NoServerFile (plan: fix_load_demotes_parse_8d40752b; artifacts: [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]], [[tests/Shared.Tests/LazyLoadReconciliationTests.fs]])

## Pending

Work ready to start but not yet claimed.

- [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] — align with git-protocol (`dev`/`ready`, no `w/*`); done forks under [[.scratch/done/update-matt-skills/forks/]] still describe `w/*`
- [[.agents/skills/git-guardrails-claude-code/SKILL.md]] — hooks may block [[scripts/merge.sh]], [[scripts/push.sh]], and cloud push of `ready`
- [[.cursor/skills/git-master/SKILL.md]] — name the tag convention on `master` and who applies a tag
- [[.scratch/llm-connector/map.md]] — chart Run `?` pack, LLM call, and write-back
- [[.scratch/roadmap/issues/07-chart-automatic-upload-and-download.md]] — chart auto-upload (and remaining pointers) for [[.scratch/roadmap/epics/work-with-text-files-from-anywhere.md]] current Chapter
- [[.scratch/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md]] — operator how-to for documents from any connected device
- [[.scratch/marketing-wiki/issues/01-use-page-documents-from-any-connected-device.md]] — first use page for documents from any connected device
- [[.scratch/document-formats/map.md]] — chart remaining document formats (XML and other draft codecs)
- [[.scratch/end-user-wiki/map.md]] — chart the end-user wiki (describe the software)
- [[.scratch/architecture/map.md]] — chart the architecture wiki (how it is coded and run)
- [[.scratch/marketing-wiki/map.md]] — chart the marketing wiki (uses, GitLab-level browsable; not a campaign)
- [[.scratch/expression-language/reports/ref-owned-children-impl.md]] — HITL: Run `= owned` and `= ref` on `/ambit` or `/ambit?debug=1` on a Node with mixed Owned and Ref Children; confirm `child` is the Children-order merge, that `owned OR ref` concatenates when roles interleave, and that `Ref` / `Owned` are unknown words
- [[.scratch/expression-language/reports/text-ops-impl.md]] — HITL: Run `= … IF (text left 5 IS "rapid")` and `= … IF (name right 4 IS ".txt")` on `/ambit` or `/ambit?debug=1`; confirm the Answers are Nodes, that a bare `left 5` reports a type error, that lowercase `is` is not the combinator, and that `"d" "e"` is a parse error
- [[.scratch/expression-language/reports/if-impl.md]] — HITL: Run `= … IF containing "…"` on `/ambit` or `/ambit?debug=1`; confirm Answers stay Nodes (not an inner stream), and that lowercase `if` is not the combinator
- [[.scratch/expression-language/reports/outer-impl.md]] — HITL: Run `= root OUTER containing "…"` on `/ambit` or `/ambit?debug=1`; confirm nested prune, Owned-only walk, and that lowercase `outer` is not the combinator
- [[.scratch/expression-language/reports/re-filter.md]] — HITL: Run `= … re "…"` and `= … rei "…"` on `/ambit` or `/ambit?debug=1`; confirm Header match, case split, and invalid pattern as no matches
- [[.scratch/expression-language/reports/run-changes-not-effective.md]] — HITL hard-reload `/ambit` or `/ambit?debug=1`; confirm Run error strings and unfold (not old red-echo)
- [[.scratch/expression-language/reports/run-commit-edit-before-exec.md]] — HITL: while Editing, change the line and Ctrl+Enter; graph text commits, then Run uses that text
- [[.scratch/expression-language/reports/expr-eval-pull-enumerator-impl.md]] — HITL: Run `= root descendant …` with more than 50 hits; confirm 50 Children and unfold
- [[.scratch/expression-language/reports/expr-eval-pull-enumerator-impl.md]] — later: adapt Expression Stream into SearchCursor / takeResults for `=` paging; do not apply the Run cap of 50
- [[.scratch/client-start-time/issues/09-cache-first-boot-delayed-lcp.md]] — HITL: `/ambit` bundle LCP of `div.amb-text` back near ~1 s (artifacts: [[.scratch/client-start-time/reports/cache-first-boot-delayed-lcp.md]])
- [[.scratch/client-start-time/issues/08-poll-hash-fallback-loop.md]] — HITL: one `/state` then Poll confirms; Selection must not jump to ROOT (artifacts: [[.scratch/client-start-time/reports/poll-hash-fallback-loop.md]])
- [[.scratch/client-start-time/reports/cold-load-loading-hang.md]] — HITL re-verify cold load past Loading..., then warm F5 cache hit (parent: [[.scratch/client-start-time/reports/implement-cache-first-boot-01-07.md]])
- [[.scratch/client-start-time/reports/page-not-responding-loading.md]] — HITL: warm F5 with occurrence-based `f` restore; legacy `e` collapses safely; confirm Zoom fallback when preferred Node is absent after replay (artifacts: [[.scratch/client-start-time/reports/restore-fold-occurrences.md]], [[src/Shared/ViewModelSiteMap.fs]], [[src/Shared/ViewModelOccurrence.fs]], [[src/Client/SessionState.fs]], [[src/Client/UpdateHelpers.fs]])
- [[.scratch/client-start-time/reports/reload-state-reuse-investigation.md]] — server revision-keyed bootstrap encode cache for warm F5 at unchanged revision (artifacts: [[src/Server/Api.fs]], [[src/Shared/ResidentProjection.fs]]; parent: [[.scratch/client-start-time/reports/state-further-optimization.md]])
- [[.scratch/client-start-time/reports/bucket-3-post-state-work.md]] — defer `restoreFoldOccurrences` to after first paint; first render collapsed SiteMap only (artifacts: [[src/Client/App.fs]], [[src/Client/SessionState.fs]]); lower priority after HITL: restore 8ms / 18 rows ([[.scratch/client-start-time/reports/production-hitl-after-deploy.md]])
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
- [[.scratch/glossary-directory-file/rename-isMarker.md]] — optional remaining speech/doc sweep for informal “marker” (Directory File sense); `isMarker` / related API renames done
- [[.scratch/large-node-cursor-perf/delete-children-cost.md]] — profile/optimize delete among large siblings (fromNodes + SiteMap rematch / structural DOM plan) (parent: [[.scratch/large-node-cursor-perf/project.md]])

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.

- [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]] — Move dialog does not offer Unloaded destinations (blocked by: [[.scratch/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]])
- [[.scratch/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] — promote implemented client residency while retaining future server residency in the roadmap (blocked by: [[.scratch/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]], [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]], [[.scratch/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]], [[.scratch/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]])
- [[.scratch/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] — dispatch full-selection Load from the hollow-circle control (blocked by: [[.scratch/selective-client-loading/issues/22-load-full-selection.md]], [[.scratch/selective-client-loading/issues/23-introduce-hollow-circle-presentation.md]])

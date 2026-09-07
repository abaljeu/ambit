# Strip leftover overview pointers

Live instruction no longer names [[plan/index.md]]. That file stays on disk with one obsolete statement. Stage of record is each `plan/<slug>/project.md`. This pass does not invent a replacement overview. [[.agents/skills/ask-matt/SKILL.md]], [[.agents/rules/gambol.md]], and [[.cursor/rules/gambol.mdc]] were out of this edit set (sibling DRY).

## What was stripped

### Live instruction

- [[plan/index.md]] — body replaced with the obsolete statement. No project catalog.
- [[.agents/skills/project-work/SKILL.md]] — dropped leftover-snapshot wording. Stage stays on `project.md`.
- [[.agents/rules/project-stage.md]] — same. Cursor stub [[.cursor/rules/project-stage.mdc]] remains an Obey pointer.
- [[.agents/skills/projects-overview/SKILL.md]] — still retired (`disable-model-invocation: true`). No leftover path. Not a regenerate job.
- [[doc/agents/project-status.md]] — dropped leftover-snapshot section. Who-writes-Stage no longer names that file.
- [[plan/roadmap/map.md]] — discovery is each Project's `project.md` plus issue Status plus wayfinder frontier. Not a stage table.

### Historical reports and issues

Dropped “see leftover catalog” / “regenerated the leftover catalog” sentences, or retargeted them to `project.md`, in:

- [[plan/client-start-time/reports/implement-cache-first-boot-01-07.md]]
- Core creation reports: chart-core-wayfinder-map, commit-16-mitigation-tickets, commit-24-and-23, commit-issue-25-followup, create-project-reorganization, grill-issue-09-launch-contract, implement-initial-core-changes, plan-initial-core-changes-implementation
- [[plan/debug-reload/reports/rehome-issue.md]]
- Delete Ref reports: fix-02-delete-owned-self-ref-hangs, self-ref-delete-owned-hang
- Event-sourced ops reports: 05-follow-up, kind-4-rename, shared-success-envelope-build
- Expression Language reports: existing-language-survey, summary-goal, text-ops-impl, and the resolve-* HITL notes
- Git protocol: [[plan/git-protocol/issues/04-named-ux-scripts.md]], implement-04, instruction-pointers, scripts-spec-tickets, stage-grilling-directive
- [[plan/large-node-cursor-perf/board-followup.md]], [[plan/large-node-cursor-perf/reports/summary-goal.md]]
- Login context restore: implement-auth-cookie, implement-session-localstorage
- [[plan/owner-edge-db-repair/implement.md]]
- [[plan/relaxed-concurrency/to-spec-report.md]]
- Roadmap reports: chapters-as-files, cursor-repo-to-ambit-mobile-grok, live-projects-and-roadmap-remainder, move-issue-14, operate-connected-channels-epic, regenerate-index-after-summaries, roadmap-issues-as-epic-chapters, webview2-azure-navigate-issue
- Skills cleanup reports: dry-ask-matt-gambol, establish-project, implement-issue-01, implement-issue-02, implement-issue-09, implement-issue-13, retire-plan-index-updates, skill-contradiction-chart, status-surface-inventory
- Work board cleanup: drop-stale-marker-sweep, retire-work-board

## Remaining hits

Search for the leftover overview path after this pass:

- [[plan/index.md]] — the obsolete statement (allowed)
- this report — names the path as the subject of the strip

Zero other files. [[.agents/skills/ask-matt/SKILL.md]] and [[.agents/rules/gambol.md]] already had no leftover overview path (sibling). Live skills, rules, `doc/agents/`, [[CONTEXT.md]], and [[plan/roadmap/map.md]] do not name that file. [[.agents/skills/projects-overview/SKILL.md]] is not a regenerate-overview job.

## Redundancy beyond this file (later)

Do not boil the ocean in this pass. Patterns that produced the ~50 leftover pointers, or that still duplicate an index:

- After every Stage or Summary change, reports restated “regenerated the leftover catalog” or “did not regenerate.” That ritual kept a second Stage table in sync with `project.md`. The table is gone; the habit was the duplication.
- The same leftover-snapshot sentence was copied into project-work, project-stage, project-status, and projects-overview. One meaning, four live homes.
- [[plan/roadmap/reports/live-projects-and-roadmap-remainder.md]] and [[plan/roadmap/reports/regenerate-index-after-summaries.md]] are themselves snapshot inventories copied from `project.md`.
- [[doc/index.md]] (Feature index of the current program) and [[plan/roadmap/map.md]] (goto for what to work on next) are two catalogs with different jobs; they still need a clean split, not a third leftover table.
- [[.agents/rules/gambol.md]] still lists the retired projects-overview skill in the job catalog. The skill’s only job was generating the leftover catalog.
- Stage values are still copied in [[plan/skills-cleanup/project.md]] Locked, [[CONTEXT.md]], [[.agents/rules/project-stage.md]], and [[doc/agents/project-status.md]] (canonical). Later DRY belongs on the Stage list, not a new overview.
- WORK.md retirement first retargeted discovery at the leftover catalog instead of `project.md`. Map is now on `project.md`; other historical WORK.md mutation sections were not rewritten.
- Dual skill homes ([[.cursor/skills/]] vs [[.agents/skills/]]) still show up as stale paths in historical reports. Skills-cleanup already owns that move.

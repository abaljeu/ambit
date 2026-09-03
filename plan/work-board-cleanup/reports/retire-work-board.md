# Retire work board

Date: 2026-09-02. No commit. No claimed tickets. No product F# for transferred work.

## Retirement (this session)

Deleted [[WORK.md]] and [[.cursor/rules/work-blackboard.mdc]]. Both are absent on disk.

Live instructions now point at [[plan/index.md]] plus that project's `issues/`. [[.cursor/rules/gambol.mdc]] no longer lists the blackboard rule or the live board. [[AGENTS.md]], [[CONTEXT.md]], [[doc/agents/]], and `.cursor/skills/` had no live board rules to change. Historical `plan/**/reports/` WORK.md mutation sections were not rewritten.

[[plan/roadmap/map.md]]: Notes retarget [[plan/work-board-cleanup/project.md]] (was work-board-audit); WORK.md is retired; discovery is [[plan/index.md]] plus issue Status plus wayfinder frontier. Out of scope no longer says “Replacing WORK.md”. Decisions so far records the retirement **without** a Roadmap issue file. [[plan/roadmap/issues/11-home-every-project-on-an-epic.md]] gist now names cleanup.

[[plan/work-board-cleanup/project.md]] Stage is `tickets`, Updated 2026-09-02, summary is cleanup not audit-only. [[plan/index.md]] regenerated. Cleanup is the only work-board row. Did not file a retirement issue; the board is gone.

`plan/work-board-audit/` is **not** on disk. Cleanup already holds the copied audit notes ([[external-multiline-paste.md]], [[warm-parse-dual-owner.md]], [[git.md]]). No duplicate folder to keep or destroy.

## Filed (unclaimed)

### git-protocol

- [[plan/git-protocol/issues/01-align-merge-to-live-with-git-protocol.md]] — Status `ready-for-agent`
- [[plan/git-protocol/issues/02-git-guardrails-may-block-ready-push.md]] — Status `ready-for-agent`
- [[plan/git-protocol/issues/03-name-master-tag-convention.md]] — Status `ready-for-agent`

### selective-client-loading

From [[tmp/load-performance-audit.md]]. Left the audit file in `tmp/`; issues are the home. Did not split onto client-start-time.

- [[plan/selective-client-loading/issues/30-ledger-reuse-on-already-synced-load.md]] — Type `grilling`, Status `open` (secondary / Mask path)
- [[plan/selective-client-loading/issues/31-skip-workspace-inventory-when-unloaded.md]] — Status `ready-for-agent` (still always POSTs inventory in [[src/Client/UpdateWorkspaceSync.fs]] `startWorkspacePush` / [[src/Client/App.fs]])
- [[plan/selective-client-loading/issues/32-defer-path-sync-ledger-waterfall.md]] — Status `ready-for-agent`

### large-node-cursor-perf

- [[plan/large-node-cursor-perf/issues/01-profile-delete-among-large-siblings.md]] — Status `ready-for-agent` (from [[plan/large-node-cursor-perf/delete-children-cost.md]]; not a cleanup issue)

### Maps — one wayfinder ticket per Not yet specified bullet

Kept existing [[plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md]] and [[plan/marketing-wiki/issues/01-use-page-documents-from-any-connected-device.md]] (Status `open`). No “chart the wiki” umbrella.

**llm-connector** (all Type `grilling`, Status `open`): [[plan/llm-connector/issues/01-how-the-pack-is-encoded.md]], [[plan/llm-connector/issues/02-which-llm-and-credentials.md]], [[plan/llm-connector/issues/03-seam-after-ask-recognition.md]], [[plan/llm-connector/issues/04-how-much-eso-before-first-ask.md]].

**document-formats** (grilling, open): [[plan/document-formats/issues/01-which-formats-besides-xml.md]], [[plan/document-formats/issues/02-expand-to-parse-vs-selective-loading.md]].

**end-user-wiki** remaining NYS (grilling, open): [[plan/end-user-wiki/issues/02-choose-wiki-home.md]], [[plan/end-user-wiki/issues/03-navigation-and-page-set.md]], [[plan/end-user-wiki/issues/04-which-doc-pages-to-link.md]], [[plan/end-user-wiki/issues/05-boundary-vs-architecture-and-marketing.md]].

**architecture** (grilling, open): [[plan/architecture/issues/01-choose-wiki-home.md]], [[plan/architecture/issues/02-gitlab-level-browsable.md]], [[plan/architecture/issues/03-cite-doc-current-without-restating.md]], [[plan/architecture/issues/04-boundary-vs-end-user-wiki-and-decisions.md]].

**marketing-wiki** remaining NYS (grilling, open): [[plan/marketing-wiki/issues/02-choose-wiki-home.md]], [[plan/marketing-wiki/issues/03-first-use-pages.md]], [[plan/marketing-wiki/issues/04-boundary-vs-end-user-wiki.md]].

Maps stay maps. No `plan/roadmap/issues/` added.

### debug-reload

- [[plan/debug-reload/issues/01-document-watch-debug-reload.md]] — Status `ready-for-agent` (from [[doc/reference/dev-debug-workflow.md]]; rehomed from work-board-cleanup 2026-09-02)

### daily-git-save

- [[plan/daily-git-save/issues/01-daily-git-save-commit-hitl.md]] — Status `ready-for-human` (rehomed from work-board-cleanup 2026-09-02)

Implement report said no commit; later commits exist (`a14dce7` / `0ab2443`). Remaining is human closeout (HITL or skip, merge to `ready`), not more coding.

### owner-edge-db-repair

- [[plan/owner-edge-db-repair/issues/01-owner-edge-db-repair-commit-hitl.md]] — Status `ready-for-human` (rehomed from work-board-cleanup 2026-09-02)

Implement report said no commit; later commit exists (`a09f35a`). Remaining is human closeout (HITL or skip, merge to `ready`), not more coding.

### work-board-cleanup leftovers

Issue 01 (optional Marker speech and doc sweep) was deleted as stale. No replacement. Did not revive [[plan/glossary-directory-file/]]. [[plan/work-board-cleanup/issues/]] is empty.

Issues 03 and 04 are not here. Those HITL tickets are now [[plan/daily-git-save/issues/01-daily-git-save-commit-hitl.md]] and [[plan/owner-edge-db-repair/issues/01-owner-edge-db-repair-commit-hitl.md]]. Number gaps are fine.

## Skipped

- Skip-inventory ticket **not** skipped: Unloaded Load still inventories.
- No Roadmap issue file for the retirement decision (map Notes/Decisions only).
- No retirement ticket left open; board deleted in this session.
- Empty [[plan/debug-reload/]] had no files at retirement. Later [[plan/debug-reload/project.md]] exists; the watch-doc issue is [[plan/debug-reload/issues/01-document-watch-debug-reload.md]].
- Did not move [[tmp/load-performance-audit.md]]; work is in selective-client-loading issues.
- Did not rewrite old reports. Did not implement transferred F#. Did not commit.

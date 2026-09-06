# Status reconsider inventory

Fact-finding only. Inventory facts stay. Status is re-locked in [[plan/skills-cleanup/reports/lock-status-set-v2.md]]. No Status lines rewritten.

Search: `rg` on `*.md` for `\*\*Status:\*\*` (next three words, then first-token dedup) and unbolded `^Status:`. Live tickets = `plan/**/issues/*.md`. Instruction tables that name roles without a `Status:` field are noted separately.

## Deduped first token (live tickets)

185 issue files have a Status field. Form: **97** `**Status:**`, **88** `Status:`.

| Value | Count | Form | Where (examples) |
| --- | --- | --- | --- |
| `resolved` | 60 | `Status:` | [[plan/childnode-drop-ref/issues/01-choose-progressive-migration-spine.md]], [[plan/roadmap/issues/01-name-and-order-first-epics.md]], [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]] |
| `ready-for-agent` | 42 | `**Status:**` | [[plan/core-creation/issues/24-clarify-core-increment-boundary.md]], [[plan/client-start-time/issues/01-persist-bootstrap-snapshot-after-state.md]], [[plan/debug-reload/issues/01-document-watch-debug-reload.md]] |
| `done` | 29 | `**Status:**` | [[plan/core-creation/issues/16-track-running-job.md]], [[plan/event-sourced-ops/issues/01-shared-success-envelope-expand.md]], [[plan/git-protocol/issues/04-named-ux-scripts.md]] |
| `open` | 25 | `Status:` | [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]], [[plan/architecture/issues/01-choose-wiki-home.md]], [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] |
| `ready-for-human` | 16 | `**Status:**` | [[plan/daily-git-save/issues/01-daily-git-save-commit-hitl.md]], [[plan/client-start-time/issues/11-hitl-cold-load-loading-hang.md]], [[plan/expression-language/issues/28-outer-prefix-combinator.md]] |
| `agent-done` | 7 | `**Status:**` | [[plan/delete-ref/issues/01-delete-any-ref-succeeds.md]], [[plan/selective-client-loading/issues/15-introduce-change-request-messaging.md]] |
| `closed` | 3 | `Status:` | [[plan/login-context-restore/issues/01-why-safari-harsh-restart-drops-auth.md]], [[plan/login-context-restore/issues/02-what-storage-survives-safari-tab-discard.md]], [[plan/login-context-restore/issues/03-httponly-cookie-options-for-safari-durability.md]] |
| `needs-info` | 2 | `**Status:**` | [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]] |
| `in-progress` | 1 | `**Status:**` | [[plan/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]] |

Two families sit on live tickets: unbolded Wayfinder-shaped `open` / `resolved` / `closed`, and bold lock-shaped `ready-for-agent` / `ready-for-human` / `done` plus extras `agent-done` and `in-progress`.

## Live tickets vs the lock

Locked list: `needs-triage` | `needs-info` | `ready-for-agent` | `ready-for-human` | `blocked` | `done` | `wontfix`. Takeable = `ready-for-agent` or `ready-for-human`. Forbidden: `open`, `claimed`, `resolved`.

On live tickets, **match the lock:** `ready-for-agent` (42), `done` (29), `ready-for-human` (16), `needs-info` (2). **Forbidden by the lock but present:** `resolved` (60), `open` (25). **Not in the lock and present:** `agent-done` (7), `closed` (3), `in-progress` (1). **In the lock, zero live tickets:** `needs-triage`, `blocked`, `wontfix`. **`claimed`:** no `Status:` field anywhere.

Live tickets do **not** match the lock. The largest closed token is `resolved`, not `done`. The largest inbox token among unbolded files is `open`, not `needs-triage` or `ready-for-agent`.

## Instruction files (not live tickets)

[[doc/agents/triage-labels.md]] lists the lock (adds `blocked` and `done` vs vendor). [[.agents/skills/setup-matt-pocock-skills/triage-labels.md]] maps five roles only: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` (no `blocked`, no `done`). [[.agents/skills/triage/SKILL.md]] (delete later) is the machine that starts unlabeled work at `needs-triage`. Ticket templates [[.agents/skills/to-tickets/SKILL.md]] and [[.agents/skills/to-feature-tickets/SKILL.md]] write `**Status:** ready-for-agent`. [[doc/agents/issue-tracker.md]] says frontier = takeable Status; claim does not change Status; resolve sets `Status: done`.

No instruction `Status:` field uses `open` or `resolved`. Those values live on tickets only.

Other `Status:` lines (specs, details, postgres roadmap checkboxes, prose “Yes. **Status:** done”) are not ticket fields. Ignore them for the enum.

## Questions (do not answer here)

1. Closed work: one token (`done`) or keep `resolved` / `closed` / `agent-done` as they already sit on tickets?
2. Inbox / takeable: keep `open`, or only `ready-for-agent` / `ready-for-human` (and migrate `open`)?
3. Drop `needs-triage` with the triage skill, or keep an unused inbox token?
4. Keep `blocked` and `wontfix` as rare tokens even though no live ticket uses them?
5. `claimed` never appears as a field — still forbid it, or drop it from the “do not use” list?

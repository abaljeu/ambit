# Implement issue 13 — Migrate live Status, bold labels, and Stage

Live tickets under [[plan/]] `issues/` now use only the locked Status set with bold field labels. Feature-set Project `Stage:` lines use `chart` | `spec` | `slice` | `build` | `done` | `dead`. The Roadmap Project file has no Stage. Product F# and [[plan/roadmap/map.md]] were not edited. This Project is delivered: [[plan/skills-cleanup/project.md]] Stage is `done`.

## What changed

- `open` → `ready-for-agent` (25 tickets). None of those tickets were HITL-only remaining work.
- [[plan/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]] `in-progress` → `ready-for-human` (agent boxes done; leftover HITL is user).
- `resolved` / `closed` / `agent-done` → `done`. No live `wontfix`.
- Unbolded `Status:` / `Type:` → `**Status:**` / `**Type:**`.
- `charting` → `chart`, `tickets` → `slice`, `active` → `build`. [[plan/auto-download-persisted-files/project.md]] `blocked` → `build` (tabled HITL, not `dead` or `done`).
- Dropped `Stage: steering` from [[plan/roadmap/project.md]].

## How verified

- Search class from [[plan/skills-cleanup/reports/status-reconsider-inventory.md]]: `^\*\*Status:\*\*` and `^Status:` on `plan/**/issues/*.md`.
- After migrate: 199 bold Status fields; unbolded `Status:` / `Type:` on live tickets = 0; forbidden first tokens (`open`, `resolved`, `claimed`, `closed`, `agent-done`, `in-progress`, `wontfix`, `needs-triage`) = 0.
- Counts: `done` 119, `ready-for-agent` 61, `ready-for-human` 17, `needs-info` 2, `blocked` 0, `cancelled` 0 (issue 13 closed after the first count of 118/62).
- Snippet: [[plan/skills-cleanup/reports/status-inventory-after-13.md]].
- `git diff --name-only -- src` empty. `plan/roadmap/map.md` unchanged.

## Leftover risks

- Unbolded `Blocked by:` and other non-Status/Type header fields remain on many Wayfinder-shaped tickets.
- Epic files under [[plan/roadmap/epics/]] still say `Stage: charting`. Out of this ticket's file list.
- [[plan/parse-load-demote/project.md]] Summary still says empty stub.
- [[plan/skills-cleanup/project.md]] Later implementation still names this migrate as future work.
- Spec files and reports still use prose `Status:` (not ticket fields).
- [[plan/selective-client-loading/issues/18-synchronize-a-resident-projection-safely.md]] keeps odd `\r\r\n` line endings from before this ticket.

# 01 — Align merge-to-live with git-protocol

**Status:** done
**Blocked by:** None — can start immediately.

## Context

[[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] still expects a clean `w/*` tip, then creates `update/mattpocock-skills`. Git protocol uses [[.cursor/skills/git-protocol/SKILL.md]] places `dev` and `ready` only. Done forks under [[plan/done/update-matt-skills/forks/]] still describe `w/*` commits.

## What to build

The merge-to-live path follows git-protocol. An agent can run the update from `dev` without a `w/*` branch. Fork skill text that still names `w/*` as the workplace is corrected or marked history.

- [x] [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] accepts `dev` (and refuses other workplaces) instead of requiring `w/*`.
- [x] [[.cursor/skills/update-matt-skills/SKILL.md]] preconditions match that script.
- [x] Fork files under [[plan/done/update-matt-skills/forks/]] that still teach `w/*` as the live workplace are updated or clearly marked as history.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md.
- 2026-09-02: Implemented. Report: [[plan/git-protocol/reports/implement-01.md]].

## See also

[[.cursor/skills/git-protocol/SKILL.md]], [[plan/done/update-matt-skills/forks/implement/SKILL.md]]

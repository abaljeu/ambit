# 01 — Move repo-shared skills to the agents home

**Status:** done
**Blocked by:** None — can start immediately.
**Actual:** 2h

## Context

A coding Agent looks up a Gambol workflow skill. Today some live skills sit under [[.cursor/skills/]] and some sit under [[.agents/skills/]]. The Agent guesses which home to open. Alan locked one home. This Project starts with that move.

## What to build

Every repo-shared workflow skill lives under [[.agents/skills/]]. Live indexes name that path. [[.cursor/skills/]] holds no live SKILL.md. The junk copy at [[.agents/skills/to-tickets - Copy/]] is gone. Chart-time lock reports may still name the old path as history. Product F# does not change.

- [x] No live workflow SKILL.md remains only under [[.cursor/skills/]].
- [x] Live indexes ([[.cursor/rules/gambol.mdc]], rules, [[CONTEXT.md]], [[doc/agents/]], live Committed Decision procedure links) name [[.agents/skills/]] for those skills.
- [x] [[.agents/skills/to-tickets - Copy/]] is gone.
- [x] Chart-time lock reports under [[plan/skills-cleanup/reports/]] are not rewritten to hide the old path.

## See also

[[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/project.md]]

## Time

- 2026-09-06 2h — moved sixteen skill directories to [[.agents/skills/]], retargeted live indexes, deleted the junk copy (from chat)

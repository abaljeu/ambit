# 01 — Leftover junk

**Status:** done
**Blocked by:** None — can start immediately.
Actual: 45m

## Context

Last night's skills-cleanup left retired skills, a dead HTML report companion, and live pointers at gone paths (`./status.sh`, `doc/plan`, missing coding-standards files, ADR-FORMAT).

## What to build

Live instruction no longer names those leftovers. Retired projects-overview is gone from disk and from any job catalog. Architecture review has no HTML-REPORT companion. Subagents start git with [[scripts/gitstatus.sh]] via git-protocol. plan-roadmap-change and code-review point at live homes. ADR-FORMAT is a Committed Decision format file.

- [x] projects-overview skill directory deleted; no live catalog line names it as a job
- [x] HTML-REPORT.md deleted under improve-codebase-architecture
- [x] core-agent-behavior points at git-protocol / gitstatus.sh, not `./status.sh`
- [x] plan-roadmap-change no longer names `doc/plan`
- [x] code-review standards sources name live `.agents/rules/` files
- [x] ADR-FORMAT renamed/retargeted to Committed Decision; short template kept

## Time

- 2026-09-07 45m — leftover junk slice (from chat)

## See also

- [[plan/skill-streamline/spec.md]]
- [[plan/skills-cleanup/reports/retire-plan-index-updates.md]]
- [[.agents/skills/prepare-agent-instruction-change/SKILL.md]]

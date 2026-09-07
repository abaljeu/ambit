# 08 — Reports land under plan reports

**Status:** done
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]
**Actual:** 45m

## Context

A coding Agent writes a research note or an architecture review. Some skills say put the file where the repo already keeps notes, or write HTML in OS temp and open it. Other Agents look under `plan/<slug>/reports/`. The notes land in two places.

## What to build

Research and architecture review write Markdown under `plan/<slug>/reports/` (current Project or the Project under review) unless a skill already names another path. Architecture review does not write HTML in OS temp and does not open a Tailwind CDN page. Subagent reports stay in that same reports folder.

- [x] Research names `plan/<slug>/reports/` unless that skill already names another path.
- [x] Architecture review writes Markdown under `plan/<slug>/reports/`. It does not write OS-temp HTML.

## See also

[[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]], [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]

## Comments

Committed Decision wording in [[.agents/skills/improve-codebase-architecture/SKILL.md]] landed here. Ticket 09 does not edit that file.

## Time

- 2026-09-06 45m — pointed research and architecture review at [[plan/]] reports Markdown (from chat)

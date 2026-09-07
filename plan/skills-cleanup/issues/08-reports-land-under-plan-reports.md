# 08 — Reports land under plan reports

**Status:** ready-for-agent
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]

## Context

A coding Agent writes a research note or an architecture review. Some skills say put the file where the repo already keeps notes, or write HTML in OS temp and open it. Other Agents look under `plan/<slug>/reports/`. The notes land in two places.

## What to build

Research and architecture review write Markdown under `plan/<slug>/reports/` (current Project or the Project under review) unless a skill already names another path. Architecture review does not write HTML in OS temp and does not open a Tailwind CDN page. Subagent reports stay in that same reports folder.

- [ ] Research names `plan/<slug>/reports/` unless that skill already names another path.
- [ ] Architecture review writes Markdown under `plan/<slug>/reports/`. It does not write OS-temp HTML.

## See also

[[plan/skills-cleanup/reports/lock-architecture-review-reports-path.md]], [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]

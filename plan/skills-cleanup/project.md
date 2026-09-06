# Skills cleanup

Stage: spec
Summary: One Stage list and one Status list, then remove skill and rule copies that contradict them.
Updated: 2026-09-06

## Core problem

We did not have one Stage list and one Status list. Ticket Status collided with Wayfinder `open|claimed|resolved`. Grilling and steering were treated as Stages. Live files and always-apply rules still use the old tokens until a later ticket.

## Destination

Canonical lists in [[doc/agents/project-status.md]] and [[doc/agents/triage-labels.md]]. Glossary names in [[CONTEXT.md]]. Later: skills and rules in [[.agents/]] and [[.cursor/]] obey those lists. Forced Stage gates wait.

## Locked

- **Stage:** `chart` | `spec` | `slice` | `build` | `done` | `dead`. Project may use all. Epic and Chapter skip `slice`. Roadmap has no Stage and no Status. Tickets have no Stage.
- **Status:** `needs-triage` | `needs-info` | `ready-for-agent` | `ready-for-human` | `blocked` | `done` | `wontfix`. Tickets only. Takeable = `ready-for-agent` or `ready-for-human`. No `open`, `claimed`, `resolved`.
- **Who writes Stage:** `/wayfinder` → `chart`; `/to-spec` → `spec`; `/to-tickets` and `/to-feature-tickets` → `slice`; first implement → `build`; delivered → `done`; abandon → `dead`. Stamp Epic/Chapter `build` when a pointed Project enters `slice` or `build`.
- **Grilling** is a method, not a field. **Archive** is an action from `done`. **Rework** is a move back. Revive from `dead` by naming a live Stage.
- Forced gates wait.

## Later implementation

When this Project edits live skills, rules, or bridges, follow [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]]. Catch-up includes [[.cursor/rules/project-stage.mdc]] (still the old grilling directive) and live `Stage:` tokens on other Projects.

## In scope

- [[.agents/skills/]]
- [[.cursor/skills/]]
- Instruction files that duplicate skill text, including vendor copies under [[.agents/skills/setup-matt-pocock-skills/]]
- [[doc/agents/]] lists and [[CONTEXT.md]] names (this increment)

## Out of scope

Rewriting product code, rewriting the whole Roadmap map body, implementing Core, ADR or Learning Record Status fields.

## Reports

- [[plan/skills-cleanup/reports/establish-project.md]] — Project established.
- [[plan/skills-cleanup/reports/status-surface-inventory.md]] — Status/Stage surface inventory.
- [[plan/skills-cleanup/reports/status-set-candidates.md]] — Early set candidates (superseded by Locked).

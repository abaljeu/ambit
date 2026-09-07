# Implement issue 08 — reports under plan reports

Research and architecture review now write Markdown under `plan/<slug>/reports/` for the current Project or the Project under review. Architecture review no longer writes OS-temp HTML or opens a Tailwind CDN page. [[.cursor/rules/core-agent-behavior.mdc]] already names that reports folder for subagents, so it was not edited. Product F# was not touched. [[.cursor/rules/gambol.mdc]] was not edited: no skill files were added or removed.

## What changed

- [[.agents/skills/research/SKILL.md]]: save the findings file under `plan/<slug>/reports/` (current Project, or the Project the question is about). The old “where the repo keeps notes / somewhere sensible” line is gone.
- [[.agents/skills/improve-codebase-architecture/SKILL.md]]: the report is Markdown in that same reports folder. No `$TMPDIR` HTML, no Tailwind CDN, no `xdg-open`. The pointer to [[.agents/skills/improve-codebase-architecture/HTML-REPORT.md]] is gone. ADR wording in this skill is now Committed Decision ([[doc/Decisions/]]), so ticket 09 does not need this file.
- Ticket [[plan/skills-cleanup/issues/08-reports-land-under-plan-reports.md]] is `Status: done`.

## How verified

- [[.agents/skills/research/SKILL.md]] contains `plan/<slug>/reports/` and does not contain “somewhere sensible”.
- [[.agents/skills/improve-codebase-architecture/SKILL.md]] contains `plan/<slug>/reports/` and does not contain `TMPDIR`, `xdg-open`, Tailwind, `HTML-REPORT`, or `.html`.
- That architecture skill contains Committed Decision and does not contain ADR.
- [[.cursor/rules/core-agent-behavior.mdc]] still names `plan/<project-name>/reports/` for subagent reports; this ticket did not change it.

## Leftover risks

- [[.agents/skills/improve-codebase-architecture/HTML-REPORT.md]] still describes OS-temp HTML and Tailwind CDN. The live skill does not point at it. Out of exclusive paths; not deleted.
- Architecture review still invokes `/grilling`. Default grill is [[.agents/skills/grill-me/SKILL.md]] per [[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]]. Not this ticket.
- [[.agents/skills/code-review/SKILL.md]] still aggregates in chat. [[.agents/skills/handoff/SKILL.md]] still uses OS temp. The grill lock named those leftovers; they are other tickets’ files.
- domain-modeling and ubiquitous-language still say ADR. Ticket 09.

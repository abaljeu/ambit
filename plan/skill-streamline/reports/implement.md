# Implement skill-streamline

All four tickets landed in one session on `dev`.

## What changed

1. **Leftover junk** — deleted `projects-overview`, `HTML-REPORT.md`, `ADR-FORMAT.md` (replaced by `COMMITTED-DECISION-FORMAT.md`). Retargeted core-agent-behavior to `scripts/gitstatus.sh`, plan-roadmap-change to live plan/roadmap, code-review standards to `.agents/rules/`.
2. **Writing pair** — moved glossary into `writing-for-agents/GLOSSARY.md`, retargeted description to AGENTS.md / `.agents/` bridges, deleted `writing-great-skills`.
3. **Design-it-twice pair** — folded requirements framing and anti-patterns into `DESIGN-IT-TWICE.md`, added trigger words to codebase-design description, deleted `design-an-interface`.
4. **List pointers** — CONTEXT Stage/Status drop pasted enums; project-stage drops Who-writes-Stage table and points at project-status.

## How verified

Repo search under `.agents/` for `projects-overview`, `./status.sh`, `doc/plan`, `writing-great-skills`, `design-an-interface`: empty. Deleted directories/files absent; new format and glossary present.

## Leftover

Historical `plan/**/reports/` and skills-cleanup Locked text still name old paths. Foreign-stack skills untouched. No product F#.

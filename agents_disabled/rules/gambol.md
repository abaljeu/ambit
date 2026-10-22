
Simpler is Better

If the IDE reports that a file became 1 line, ignore the false report; don't investigate.

Canonical project rules are split across [[.agents/rules/]]. Cursor attaches them through thin stubs in [[.cursor/rules/]] (`alwaysApply` or `globs`).

- [[.agents/rules/core-agent-behavior.md]] — rules for every agent
- [[.agents/rules/main-agent.md]] — rules for the main agent
- [[.agents/rules/project-values.md]] — project aims and stack
READ EXACTLY ONE OF THESE TWO:
- [[.agents/rules/environment-windows-cursor-ide.md]] — Windows Cursor IDE (shell, paths, tooling)
- [[.agents/rules/environment-cursor-cloud.md]] — Cursor Cloud (toolchain, Postgres, runtime)


Do no work on the git branch staging.
- [[.agents/rules/fsharp-source.md]] — F# conventions (scoped to *.fs)
- [[.agents/rules/core-api.md]] — Core vs Adapter pointers (scoped to Server Core / Adapter)
- [[.agents/rules/markdown-writing.md]] — markdown conventions (scoped to *.md)
- [[.agents/rules/planning-docs.md]] — roadmap/plan documents (scoped to doc/)
- [[.agents/rules/project-stage.md]] — keep each `plan/` project's `Stage:` current
- [[.agents/rules/no-retrofit.md]] — skill or rule change; old work stays
- [[.agents/rules/refer-by-name.md]] — number and name projects, issues, sections, list items; title wraps the link

## Glossary

This repo is named Ambit.  The software is Ambit.  This file specifies words and phrases to use or not use.

- [[GLOSSARY.md]] — concise domain glossary

Skills
[[.agents/skills/CONTEXT.md]]

Tool-specific notes:

- Codex: [[.agents/codex-context.md]]
- Copilot: [[.agents/copilot-instructions.md]]

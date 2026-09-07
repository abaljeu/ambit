# Migrate remainder of `.cursor/` to `.agents/`

Extra pass after tickets 01–12. Ticket 01 already moved live workflow `SKILL.md`. This pass moved remaining repo-shared agent material. Product F# was not touched. Live `plan/**/issues/` Status/Stage, [[plan/skills-cleanup/issues/13-migrate-live-status-labels-and-stage.md]], and [[plan/skills-cleanup/reports/implement-issue-13.md]] were not edited.

## Inventory (before)

Tracked under `.cursor/`: nine `.mdc` rules, [[.cursor/codex-context.md]], [[.cursor/copilot-instructions.md]], and four Cursor plan files. `.cursor/skills/` was an empty leftover directory (no `SKILL.md`; ticket 10 leftover copies were already gone). No `mcp.json`, hooks, or IDE settings in the repo.

## Moved

Canonical policy now lives under [[.agents/rules/]] as `.md` (Cursor frontmatter stripped):

- [[.agents/rules/gambol.md]] — primary Agent instruction index
- [[.agents/rules/core-agent-behavior.md]]
- [[.agents/rules/project-values.md]]
- [[.agents/rules/environment.md]]
- [[.agents/rules/fsharp-source.md]]
- [[.agents/rules/core-api.md]]
- [[.agents/rules/markdown-writing.md]]
- [[.agents/rules/planning-docs.md]]
- [[.agents/rules/project-stage.md]]

Bridges: [[.agents/codex-context.md]], [[.agents/copilot-instructions.md]].

`.cursor/skills/` is gone (empty; nothing to move).

Ticket 10 leftovers in live files: compile-gate pointers in [[.agents/rules/core-agent-behavior.md]] and [[.agents/skills/investigate-fable-client/SKILL.md]] now name [[.agents/skills/implement-fsharp-feature/SKILL.md]] instead of deleted `testing-workflow`.

## Stubs (Cursor still loads these)

[[.cursor/rules/]] keeps the nine `.mdc` files with the original `alwaysApply` / `globs` frontmatter and a one-line `Obey [[.agents/rules/…]]` body. Cursor still attaches them. Policy is not inlined; the agent must follow the link (same pattern as [[AGENTS.md]]).

[[.cursor/codex-context.md]] and [[.cursor/copilot-instructions.md]] are the same kind of stub.

## Stayed (not this commit)

- **`.cursor/plans/`** — Cursor IDE plan files. Another dirty tree already deleted them and added [[plan/oldplans/]]. Not stolen.
- **No mcp/hooks/settings** in the repo, so nothing to stub there.
- Empty `.cursor/skills/` copies naming testing-workflow: already absent.

## Live pointers retargeted

[[AGENTS.md]], [[.agents/rules/gambol.md]], [[.agents/skills/ask-matt/SKILL.md]], [[.agents/skills/prepare-agent-instruction-change/SKILL.md]], Follow lines on the skills that named `.cursor/rules/`, and [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]. Live gambol/core-agent-behavior wikilinks on [[plan/skills-cleanup/project.md]] were retargeted in this window and landed on `dev` with the ticket 13 commit; this commit adds the files those links name. [[CONTEXT.md]] and [[doc/agents/]] had no `.cursor/rules` links. Chart-time lock reports keep old paths.

## Skill flaws (not repaired)

- [[.agents/rules/core-agent-behavior.md]] still names `./status.sh`. Another dirty tree is replacing that with [[scripts/gitstatus.sh]]; this commit does not steal that.
- [[.agents/skills/git-protocol/SKILL.md]] is dirty; not included.
- Who-writes-Stage still lists only `/wayfinder` → `chart` (request-refactor-plan leftover).
- Prepare previously assumed rules were canonical in `.cursor/rules/`. That skill now describes stubs; other skills were not rewritten beyond path retarget.

## How verified

- `git ls-files .cursor/rules` still lists the nine `.mdc` stubs with frontmatter.
- No live `SKILL.md` Follow line names `.cursor/rules/` or `testing-workflow`.
- `git diff --name-only` for this commit does not include `plan/**/issues/`, git-protocol, `status.sh`, or `.cursor/plans/`.

# Draft — scratch-script skill

## Path

[[.cursor/skills/scratch-script/SKILL.md]]

## Why that location

[[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] puts repo-shared skills under [[.cursor/skills/]], not [[.agents/skills/]]. This is a Gambol workflow the agent must reach on its own, so the skill is model-invoked (it has a `description`).

## Pointers

One line added to [[.cursor/rules/gambol.mdc]] under Workflow skills. That file is the inventory; prepare-agent-instruction-change asks for an update when a skill file is added. Did not copy the body into [[AGENTS.md]] (it already points at gambol.mdc).

## What it forces

Write the same commands you would have run as a short scratch `.sh` (Write tool, newlines), run that file, then delete it.

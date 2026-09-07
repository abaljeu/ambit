---
name: prepare-agent-instruction-change
description: Maintains Gambol agent instructions by editing rules, skills, and tool bridge files without duplication. Use when changing .cursor/rules, .cursor/skills, AGENTS.md, .cursor/codex-context.md, or .cursor/copilot-instructions.md.
---

# Prepare Agent Instruction Change

Canonical layout: [[.cursor/rules/gambol.mdc]].

## Principles

- **Rules** — policy and file conventions (`alwaysApply` or `globs`).
- **Skills** — recurring workflows; link to rules, do not copy them.
- **Bridges** — tool-specific deltas only (`AGENTS.md`, `.cursor/copilot-instructions.md`, `.cursor/codex-context.md`).

## Edit workflow

1. Inventory: universal rule, scoped rule, skill, or bridge?
2. Edit the most specific location; remove duplicated text elsewhere.
3. Update [[.cursor/rules/gambol.mdc]] if files are added or removed.

## Review before finishing

- [ ] No contradictions across rules and skills.
- [ ] Universal rules stay short; scoped rules do not repeat them.
- [ ] Skills link to rules instead of copying policy.
- [ ] Bridge files do not duplicate rule or skill bodies.

## Do not

- Change application source code unless explicitly requested.
- Create skills under `~/.cursor/skills-cursor/` (Cursor-managed).
- Store repo-shared skills in `.agents/skills/` — use `.cursor/skills/`.

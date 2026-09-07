---
name: prepare-agent-instruction-change
description: Maintains Gambol agent instructions by editing rules, skills, and tool bridge files without duplication. Use when changing .agents/rules, .agents/skills, AGENTS.md, .agents/codex-context.md, or .agents/copilot-instructions.md.
---

# Prepare Agent Instruction Change

Canonical layout: [[.agents/rules/gambol.md]].

## Principles

- **Rules** — policy and file conventions. Canonical text lives in [[.agents/rules/]]. Cursor loads [[.cursor/rules/]] `.mdc` stubs (`alwaysApply` or `globs`) that point at those files.
- **Skills** — recurring workflows; link to rules, do not copy them. Repo-shared skills live in [[.agents/skills/]].
- **Bridges** — tool-specific deltas only (`AGENTS.md`, [[.agents/copilot-instructions.md]], [[.agents/codex-context.md]]). Cursor still reads [[.cursor/copilot-instructions.md]] and [[.cursor/codex-context.md]] as stubs.

## Edit workflow

1. Inventory: universal rule, scoped rule, skill, or bridge?
2. Edit the most specific location; remove duplicated text elsewhere.
3. Update [[.agents/rules/gambol.md]] if files are added or removed. Keep the matching `.cursor/rules/` stub's frontmatter if a rule is added or removed.

## Review before finishing

- [ ] No contradictions across rules and skills.
- [ ] Universal rules stay short; scoped rules do not repeat them.
- [ ] Skills link to rules instead of copying policy.
- [ ] Bridge files do not duplicate rule or skill bodies.

## Do not

- Change application source code unless explicitly requested.
- Create skills under `~/.cursor/skills-cursor/` (Cursor-managed).

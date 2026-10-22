---
name: prepare-agent-instruction-change
description: Maintains Gambol agent instructions by editing rules, skills, and tool bridge files without duplication. Use when changing .agents/rules, .agents/skills, AGENTS.md, .agents/codex-context.md, or .agents/copilot-instructions.md.
---

# Prepare Agent Instruction Change

Layout and homes: [[PRINCIPLES.md]].

## Edit workflow

1. Inventory: universal rule, scoped rule, skill, or bridge?
   Done: the change is classified as exactly one of those four.
2. Edit the most specific location; remove duplicated text elsewhere. Follow [[PRINCIPLES.md]]. Edit application source only when Alan asks. Put new shared skills in [[.agents/skills/]].
   Done: one normative home holds the change; other files cite or omit; no contradictions across rules and skills; universal rules stay short and scoped rules do not repeat them; [[PRINCIPLES.md]] holds; when the change includes a skill SKILL.md, [[.agents/skills/writing-for-agents/SKILL.md]] Recipe §4 Done holds.
3. Update [[.agents/rules/gambol.md]] if files are added or removed. Keep the matching `.cursor/rules/` stub's frontmatter if a rule is added or removed.
   Done: gambol.md lists match adds and removes; each added or removed rule has matching stub frontmatter in sync.

# New-artifacts meta rule

Date: 2026-09-13

## SoT

[[.agents/rules/new-artifacts.md]] — always-apply policy. One file because the rule covers plan artifacts and later skill/template use. Not copied into to-tickets, to-spec, implement, code-review, git-protocol, project-work, or prepare-agent-instruction-change. [[.agents/skills/writing-for-agents/SKILL.md]] unchanged (always-apply loads the rule; no pointer required).

## Exact text added

[[.agents/rules/new-artifacts.md]]:

```
# New artifacts

When a process, a template, or a skill changes, apply it to new artifacts that you create after the change.

Keep existing tickets, specs, reports, skills-as-examples, and other already-written work in their current shape. Rewrite them only when the user asks.

Edit the process document to change the process. That edit is not a rewrite of old work.
```

[[.cursor/rules/new-artifacts.mdc]] (`alwaysApply: true`):

```
Obey [[.agents/rules/new-artifacts.md]].
```

## gambol.md / stub

Catalog line added under the rules list in [[.agents/rules/gambol.md]] (file added):

`- [[.agents/rules/new-artifacts.md]] — apply a changed process to new artifacts only`

No rule file removed. No other catalog or stub edits.

## Not done

No application source. No commits. No rewrite of existing tickets, specs, reports, or skills. Did not touch [[plan/core-creation/reports/skill-structure-vs-method.md]]. Did not start a ship-pipeline split.

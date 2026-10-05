# Adaptations

Upstream is mattpocock/skills `skills/engineering/retro` (v1.3). This skill is user-invoked. The human picks before any edit. Steps live in [[SKILL.md]].

## Kept

- Seven places: navigation, automated checks, coding standards, global instructions, tool economy, no-ops, information access.
- A mechanical miss becomes a deterministic check. A judgement call stays prose.
- Ranked proposal. No edit until the human picks.
- Place in the path: after code-review and [[.agents/skills/pr/SKILL.md]], when the human invokes this skill.

## Skipped or redirected

- `CODING_STANDARDS.md` is not a file in this repo. Judgement rules live in [[.agents/rules/]]. Review procedure is [[.agents/skills/code-review/SKILL.md]]. Smells live in [[.agents/skills/code-review/SMELLS.md]]. The mechanical scan is `standards-scan.py`.
- [[AGENTS.md]] is a one-line pointer to [[.agents/rules/gambol.md]]. Do not grow it. Catalog edits go in gambol.md through [[.agents/skills/prepare-agent-instruction-change/SKILL.md]].
- Checks to read first are `dotnet test`, the code-review scan, and CI. `package.json` is not the primary check list.
- Product deepening stays [[.agents/skills/improve-codebase-architecture/SKILL.md]]. [[.agents/skills/diagnosing-bugs/SKILL.md]] Phase 6 already hands architecture to that skill. This change does not replace that handoff.
- [[.agents/skills/handoff/SKILL.md]] carries a session to another harness. This skill does not write a handoff.
- [[.agents/skills/maintain-doc-currency/SKILL.md]] owns `doc/` placement. The default target here is the agent environment under `.agents/`.
- [[.agents/skills/resolving-merge-conflicts/SKILL.md]] stays. Upstream deleted that skill. Ambit did not.
- Glossary: same boundary as [[.agents/skills/pr/ADAPTATIONS.md]]. Do not run the upstream `CONTEXT.md` rename.
- Upstream says "Call the Skill tool". This repo points at skills with wikilinks.
- [[.agents/rules/no-retrofit.md]]: a picked edit changes the process document. It does not rewrite old tickets, specs, or reports.
- [[.agents/skills/loop-me/SKILL.md]] grills life workflows. It is not this retrospective.
- No Jev step. Jev stays on [[.agents/skills/pr/SKILL.md]].
- [[.agents/skills/ask-matt/SKILL.md]] does not copy the catalog, so this change does not edit ask-matt. The path line is in [[.agents/rules/gambol.md]]. [[.agents/skills/implement/SKILL.md]] already names code-review, so that check step has one pointer.

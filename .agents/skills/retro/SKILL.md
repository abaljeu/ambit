---
name: retro
description: "Retrospective after a coding session. Propose agent-environment changes. Change nothing until the human picks."
disable-model-invocation: true
---

Suggest changes to the agent environment. Product code stays out of this skill. The human picks before any edit. When the human asks, run this after [[.agents/skills/code-review/SKILL.md]] and [[.agents/skills/pr/SKILL.md]]. Clash notes: [[ADAPTATIONS.md]].

## Process

### 1. Load the writing rules

Follow [[.agents/skills/writing-for-agents/SKILL.md]] for a later instruction edit. Follow [[.agents/skills/prepare-agent-instruction-change/SKILL.md]] when a picked item edits [[.agents/rules/]], [[.agents/skills/]], [[AGENTS.md]], or a tool bridge. Do not edit in this step. Done: those skills are in hand, and no file has changed.

### 2. Read the session

Read the session the human names. When the human names none, read the current session. Read the diff and the skills that session used. Done: you can name the session and the friction in it.

### 3. Find candidates

Look in these seven places. Put each candidate in one place.

1. **Navigation** — the agent was slow to find a file, or a dependency was hidden. A navigation pointer is the usual fix.
2. **Automated checks** — a mistake a check could catch, or the repo has no guardrail. Read the existing checks first: `dotnet test`, [[.agents/skills/code-review/SKILL.md]] `standards-scan.py`, and CI. A check that exists but did not run is the finding. Do not invent a second checker for the same rule.
3. **Coding standards** — the reviewer missed a mistake. A mechanical miss (a fixed pattern, a banned call, an import shape, a file location) becomes a deterministic check. A judgement call stays in [[.agents/rules/]] and in [[.agents/skills/code-review/SMELLS.md]]. Do not create `CODING_STANDARDS.md`. The reviewer holds that standard. The implementer does not receive a new prose rule for the same miss.
4. **Global instructions** — a steering line belongs in a narrower rule or in a check. [[AGENTS.md]] stays a short pointer. The catalog is [[.agents/rules/gambol.md]]. Do not grow [[AGENTS.md]].
5. **Tool economy** — an expensive tool call has a cheaper path.
6. **No-ops** — a steering sentence does not change behaviour. Delete that sentence when the human picks it ([[.agents/skills/writing-for-agents/SKILL.md]]).
7. **Information access** — a fact the agent needed was not available.

Send other findings to their owner:

- Product module shape goes to [[.agents/skills/improve-codebase-architecture/SKILL.md]]. [[.agents/skills/diagnosing-bugs/SKILL.md]] Phase 6 already sends architecture there.
- `doc/` placement goes to [[.agents/skills/maintain-doc-currency/SKILL.md]].
- Git procedure stays [[.agents/skills/git-protocol/SKILL.md]]. Leave [[.agents/skills/resolving-merge-conflicts/SKILL.md]] in place.
- Read domain words from [[GLOSSARY.md]]. Do not rename [[GLOSSARY.md]] or `CONTEXT.md`.

Done: each candidate has one place, a severity, and the file that would change. Each out-of-scope item names its owner.

### 4. Present the list

Give the human the candidates, most serious first. Each item says the change and the home. Change nothing in this step. Done: the human has the ranked list, and no skill, rule, doc, or product file was edited.

### 5. Apply only a picked item

Edit only the items the human picks. Follow [[.agents/rules/no-retrofit.md]]. A process edit does not rewrite old tickets, specs, or reports. A picked product defect becomes a ticket through [[.agents/skills/project-work/SKILL.md]]. It is not a silent product edit. When the human picks nothing, leave the tree unchanged. Done: the tree matches the picks, or the tree is unchanged.

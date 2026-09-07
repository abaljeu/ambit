# Lock: grill default, reports path, refactor-plan, glossary

Earlier locks stay (Q1–Q5, vendor merge obsolete). Keep QA is locked in [[plan/skills-cleanup/reports/lock-keep-qa.md]]. Implement path is locked in [[plan/skills-cleanup/reports/lock-implement-path.md]]. Triage deletion is locked in [[plan/skills-cleanup/reports/lock-delete-triage.md]].

**Default grill.** The default grill entry in this repo is [[.agents/skills/grill-me/SKILL.md]]. grilling, grill-with-docs, wait-what, and loop-me are not the default invoke. Grilling remains a method, not a Stage.

**Reports path.** Yes: Gambol reports live under `plan/<slug>/reports/` unless a skill names another path. Matches [[.cursor/rules/core-agent-behavior.mdc]] subagent reports.

**Request-refactor-plan.** [[.agents/skills/request-refactor-plan/SKILL.md]] is like Wayfinder: a process that creates a new Project under `plan/` and charts it. It is not a third spec/ticket skill in a linear wayfinder→to-spec→tickets sequence. Structure of the new Project follows [[doc/agents/issue-tracker.md]] and [[.cursor/skills/project-work/SKILL.md]] (reference, do not repeat).

**One glossary.** [[.agents/skills/domain-modeling/SKILL.md]] writes [[CONTEXT.md]]. Always say **Committed Decision**, not ADR. [[.agents/skills/ubiquitous-language/SKILL.md]] is not a second glossary owner.

Later obedience punch list (do not do in this charting pass):

- Ask-Matt still starts with `/grill-with-docs` in a working directory; retarget the default to `/grill-me` so it agrees with gambol.mdc (Q1).
- Point research, code-review, and handoff leftovers at `plan/<slug>/reports/` unless a skill already names another path.
- Make request-refactor-plan reference the tracker and project-work instead of copying Project structure.
- Change ADR wording in skills to Committed Decision. Do not create UBIQUITOUS_LANGUAGE.md.

This pass did not edit skill or rule bodies.

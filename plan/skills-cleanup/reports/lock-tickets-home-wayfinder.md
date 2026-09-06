# Lock: both ticket skills, skill home, Wayfinder vs tracker

Q1 stays: Ask-Matt is the human advisor; [[.cursor/rules/gambol.mdc]] is the primary agent instruction file; they must agree. These three locks do not reopen that.

**Both ticket skills.** Keep [[.agents/skills/to-tickets/SKILL.md]] (tracer-bullet vertical slices) and [[.agents/skills/to-feature-tickets/SKILL.md]] (cohesive testable capabilities). Later destination: refactor the common core (shared publish path, template, tracker wiring) to reduce duplicates. That refactor is not work now.

**Skill home.** Repo-shared skills live in [[.agents/skills/]]. Not [[.cursor/skills/]] as the home. Today's split is not the destination.

**Wayfinder and tracker.** [[.agents/skills/wayfinder/SKILL.md]] must reference and not repeat [[doc/agents/issue-tracker.md]]. Wayfinder is a process (when to research, grill, or prototype; how to chart a destination). Issue-tracker is a structure and wins on claim, Type, Status, frontier, and file layout.

Later obedience punch list (do not do in this charting pass):

- Extract the ticket-skill common core; leave both named skills.
- Retarget or move workflow skills so the live home is [[.agents/skills/]]. Change [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] (it currently forbids that home) and the workflow-vs-ticket split in [[.cursor/rules/gambol.mdc]].
- Edit the Wayfinder body so it points at [[doc/agents/issue-tracker.md]] and drops copied GitHub-shaped ops.
- Ask-Matt must name both ticket skills so it agrees with gambol.mdc (Q1 punch list).

Question 5 is locked in [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]] (all work on `dev`). This pass did not edit skill or rule bodies.

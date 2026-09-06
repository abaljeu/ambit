# Establish Project

Established [[plan/skills-cleanup/]] as a Feature-set Project. Work stayed on **dev**. No commit. No remotes.

Alan's correction landed during this establish pass (the directory did not exist yet). The first write of [[plan/skills-cleanup/project.md]] already names the flaw as a core problem, not a footnote.

## Stage

`charting`. New effort. Destination is named at low resolution. Route is not ticketed.

Assumption vs `grilling`: [[doc/agents/project-status.md]] uses `grilling` only as a directive to run [[.agents/skills/grilling/SKILL.md]]. The user asked not to start grilling. `charting` is the status for a new effort whose destination is known at low resolution. The next agent that starts this Project does not enter a grill unless Stage is later set to `grilling`.

## Core problem (correction)

We do not have a complete and coherent status definition set across all uses of status information. The Project must produce one vocabulary, or an explicit non-colliding split, that covers every surface that says Status, Stage, or equivalent next-action and lifecycle words.

The Wayfinder frontier (`Status: open|claimed|resolved`) vs triage roles (`ready-for-agent` in [[doc/agents/triage-labels.md]]) is evidence, not the destination. That clash is one symptom of the missing definition set.

## Files created or changed

- [[plan/skills-cleanup/project.md]] — created. Stage `charting`. Summary, Core problem, Destination, later implementation pointer, in/out of scope. No `Started:`. No `Actual:`. Did not create git.md.
- [[plan/index.md]] — regenerated: listed every `plan/*/` except `done/`, read `Stage:` and `Summary:` from each `project.md`, wrote one row per directory in vocabulary order then name. Added the Skills cleanup row under `charting`.
- [[plan/skills-cleanup/reports/establish-project.md]] — this report.

Not created: [[plan/skills-cleanup/notes.md]] (project.md is sufficient). Not created: [[plan/skills-cleanup/map.md]] (Destination is two lines on project.md; no tickets invented).

## Later implementation path

Live skill and rule edits, when this Project reaches implementation, follow [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]]. That skill was not applied now.

## What this pass did not do

- Did not start grilling.
- Did not write tickets.
- Did not edit live skills, rules, or [[doc/agents/]] instruction files.
- Did not inventory every Status use in the repo.
- Did not commit.
- Did not run remotes.
- Did not create a second overlapping Project.

# Project stage

Every **project** — a `.scratch/<slug>/` effort — carries one lifecycle **stage**. This file is the single source of truth for the stage vocabulary, the per-project `project.md`, and the overview index.

A project's `Stage:` is distinct from an issue's `Status:` line, which records a triage role (see [[doc/agents/issue-tracker.md]]). Exception: `Status: grilling` or `Stage: grilling` on an issue is the grilling directive for that issue, not a triage role. An Epic also has a **Stage** (same words as a feature-set Project, except `steering`). **User Epics** have **Chapters** (named beats) plus **Required for done**; **Developer Epics** have Required for done and no Chapters. Record Epic Stage on the Epic file. The Roadmap map groups Epics by Stage; they are not rows in [[.scratch/index.md]]. Advancing a Chapter means charting pointers to other Projects’ pieces, not coding on the Roadmap.

## project.md

Each project directory holds a `project.md`:

```
# <name>

Stage: <stage>
Summary: <one line state the goal of the project.  Use [[.agents/skills/wait-what/SKILL.md]]>
Updated: <YYYY-MM-DD>
```

## Stages

Grounded in the wayfinder arc ([[.agents/skills/wayfinder/SKILL.md]]) and the issue tracker:

| Stage | Meaning |
| --- | --- |
| `grilling` | directive: the next agent that starts or advances this project must follow [[.agents/skills/grilling/SKILL.md]]; not status-only. After grilling starts, set `charting`. |
| `charting` | destination unnamed; grilling or wayfinder mapping the frontier |
| `steering` | standing Project that sequences Epics toward the application; not a bounded feature-set destination |
| `spec` | destination reached (spec, plan, or decision); not yet broken into issues |
| `tickets` | broken into implementation issues; ready to build |
| `active` | implementation underway |
| `blocked` | waiting on a named dependency or decision |
| `done` | delivered; awaiting cleanup or removal |
| `dead` | retired without delivery; will not resume |

## Setting the stage

Whenever a skill advances a project's **stage** — grilling or wayfinder names a destination, a spec or refactor plan locks, `to-tickets` breaks it down, implementation starts or finishes — set `Stage:` in that project's `project.md`, refresh `Updated:`, then regenerate the overview. Create `project.md` if the effort lacks one.

`grilling` is the only stage that invokes a skill. When you start or advance a project and `Stage:` is `grilling`, follow [[.agents/skills/grilling/SKILL.md]]. Stay in the interview: do not implement, ticket, or skip it. After grilling starts, set `charting`. An agent already working a different issue of the same project does not stop. Other stages are status-only.

## Overview

[[.scratch/index.md]] is a regenerated table of every live project's name, stage, and summary. Regenerate it from the `project.md` files with [[.cursor/skills/projects-overview/SKILL.md]] after any stage change. Never hand-maintain its rows.

## Archive

`.scratch/done/` holds archived projects and is not itself a project — the overview skips it. Once a project reaches `done`, [[.cursor/skills/to-archive/SKILL.md]] moves `.scratch/<slug>/` to `.scratch/done/<slug>/` and drops it from the overview.

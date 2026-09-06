# Stage

This file is the single source of truth for the **Stage** list. Glossary names: [[CONTEXT.md]]. Ticket **Status** is [[triage-labels.md]]. Tracker operations: [[issue-tracker.md]].

## Who carries Stage

| Entity | `Stage:` | `Status:` | May be `slice` |
| --- | --- | --- | --- |
| Feature-set Project | yes | no | yes |
| Epic | yes | no | no |
| Chapter | yes | no | no |
| Ticket | no | yes | — |
| Roadmap | no | no | — |

Record Project Stage on `plan/<slug>/project.md`. Record Epic Stage on the Epic file. Record Chapter Stage on the Chapter file. The Roadmap map groups Epics by Stage; Epics are not rows in [[plan/index.md]].

**Grilling** is a method, not a Stage. Use it when a concept is already clear. **Archive** is an action from `done` ([[.cursor/skills/to-archive/SKILL.md]]), not a Stage. **Rework** is a move back to a live Stage, not a value. Forced skill gates wait; skills write Stage and do not refuse work by Stage yet.

## project.md

Each feature-set Project directory holds a `project.md`:

```
# <name>

Stage: <stage>
Summary: <one line state the goal of the project.  Use [[.agents/skills/wait-what/SKILL.md]]>
Updated: <YYYY-MM-DD>
Started: <YYYY-MM-DD>   # optional until known; set from chat or first build commit
Finished: <YYYY-MM-DD>  # when Stage is done; omit while live; omit on dead
Actual: <Nh>            # optional; sum of issue ## Time under this project
```

Time arc: see [[issue-tracker.md]] (Time tracking). Fill `Started` / `Finished` / `Actual` from conversation handoffs and commits when missing.

## Stage list

| Stage | Meaning |
| --- | --- |
| `chart` | Scope the effort. Wayfinder writes the charter (scope and explore). |
| `spec` | Tight spec. On an Epic or Chapter: fill Context, Goal, and pointers to Projects or tickets. |
| `slice` | Project only. Sequence implementation increments after spec. |
| `build` | Implementing. On an Epic or Chapter: stamp when a pointed Project enters `slice` or `build`. |
| `done` | Delivered. |
| `dead` | Abandoned. Replaces the live Stage. Revive by setting a live Stage (`chart`, `spec`, `slice`, or `build`). |

## Who writes Stage

Set `Stage:` and `Updated:`, then regenerate [[plan/index.md]] with [[.cursor/skills/projects-overview/SKILL.md]] when a feature-set Project Stage changes. Create `project.md` if the effort lacks one.

| Skill or act | Stage |
| --- | --- |
| `/wayfinder` on a bounded effort | `chart` |
| `/to-spec` | `spec` |
| `/to-tickets`, `/to-feature-tickets` | `slice` |
| First implement | `build` |
| Delivered | `done` |
| Abandon | `dead` |

Epic and Chapter never run `/to-tickets`. Stamp `build` when a pointed Project enters `slice` or `build`. The Roadmap writes neither field.

## Overview

[[plan/index.md]] is a regenerated table of every live feature-set Project's name, stage, and summary. Regenerate it from the `project.md` files with [[.cursor/skills/projects-overview/SKILL.md]] after any stage change. Never hand-maintain its rows. The Roadmap is not a Stage row of this vocabulary.

## Archive

`plan/done/` holds archived projects and is not itself a project — the overview skips it. Once a project reaches `done`, [[.cursor/skills/to-archive/SKILL.md]] moves `plan/<slug>/` to `plan/done/<slug>/` and drops it from the overview.
